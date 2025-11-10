using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZurfurGuiGen;

[Generator]
public class GenerateZui : IIncrementalGenerator
{

    // NOTE: We can't use record class here because code generators must target netstandard2.0
    class FileInfo
    {
        public string Path { get; set; } = "";
        public string FileName { get; set; } = "";

        /// <summary>
        /// If present, any of the data below may be missing or invalid.
        /// </summary>
        public Diagnostic? Diagnostic { get; set; }

        /// <summary>
        /// The parsed JSON document (or empty if parsing failed)
        /// </summary>
        public Dictionary<string, object?> JsonDocument { get; set; } = new();

        /// <summary>
        /// The controller or style name value from the JSON document (of "" if missing)
        /// </summary>
        public string JsonName = "";

        /// <summary>
        /// Namespace extracted from the .cs file (or "" if none)
        /// </summary>
        public string Namespace { get; set; } = "";

        public string NamespaceFileName => $"{Namespace}.{FileName}";
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect zuiData for all ZUI.JSON files
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());
        var zuiData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
        {
            var text = combined.Left;
            var syntax = combined.Right;
            return CollectJsonFiles(text, syntax, cancellationToken);
        });

        // Collect zssData for all ZSS.JSON files
        var zssData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zss.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
            {
                var text = combined.Left;
                var syntax = combined.Right;
                return CollectJsonFiles(text, syntax, cancellationToken);
            });

        // Generate controller classes 
        context.RegisterSourceOutput(zuiData.Collect(), GenerateControllerClasses);

        // Generate the ZurfurMain partial class in each project
        context.RegisterSourceOutput(zuiData.Collect().Combine(zssData.Collect())
            .Combine(context.CompilationProvider), (sourceProductionContext, triple) =>
        {
            var collectedZuiData = triple.Left.Left;
            var collectedZssData = triple.Left.Right;
            var compilation = triple.Right;
            GenerateZurfurMain(sourceProductionContext, compilation, collectedZuiData, collectedZssData);
        });
    }

    /// <summary>
    /// Collect FileInfo from a .json file.
    /// If a corresponding .cs file exists, collect the namespace from it.
    /// </summary>
    static FileInfo CollectJsonFiles(AdditionalText text, SyntaxTree[] syntax, CancellationToken cancellationToken)
    {
        var zuiPath = text.Path;
        var fileNameWithoutJsonExt = Path.GetFileNameWithoutExtension(zuiPath); // removes .json

        // Try to find the .cs file with the same name as the JSON file
        var csTree = syntax.FirstOrDefault(tree =>
        {
            var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
            return string.Equals(csFileName, fileNameWithoutJsonExt, StringComparison.OrdinalIgnoreCase);
        });

        // Parse the json file (errors are collected into diagnostic)
        Diagnostic? diagnostic = null;
        Dictionary<string, object?> jsonDocument = new();
        var jsonName = "";
        var nameSpace = GetNameSpace(csTree) ?? "";
        var fileNameOnly = Path.GetFileNameWithoutExtension(fileNameWithoutJsonExt); // removes .zui or .zss
        try
        {
            // Parse the JSON content, find the controller or style name
            jsonDocument = Json.Parse(text.GetText(cancellationToken)?.ToString() ?? "");
            if (Path.GetExtension(fileNameWithoutJsonExt).ToLower() == ".zss")
                jsonName = GetStyleName(fileNameOnly, jsonDocument);
            else
                jsonName = GetControllerName(fileNameOnly, csTree != null, nameSpace, jsonDocument);
        }
        catch (LocationException lex)
        {
            // Report a diagnostic error if there is a LocationException while parsing
            var errorLocation = Location.Create(zuiPath, new TextSpan(0, 0),
                new LinePositionSpan(new LinePosition(lex.Line, lex.Column), new LinePosition(lex.Line, lex.Column)));
            diagnostic = GetDiagnostic(errorLocation,
                "ZUI001", "ZUI JSON Parsing Error",
                $"Error while parsing JSON '{Path.GetFileName(zuiPath)}': {lex.Message}");
        }
        catch (Exception ex)
        {
            // Report a diagnostic error if there is an exception while parsing
            var errorLocationTop = Location.Create(zuiPath,
                new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
            diagnostic = GetDiagnostic(errorLocationTop, "ZUI002", "ZUI Code Generation Error",
                $"Error while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}");
        }

        return new FileInfo
        {
            Path = zuiPath,
            FileName = fileNameOnly,
            Diagnostic = diagnostic,
            JsonDocument = jsonDocument,
            JsonName = jsonName,
            Namespace = nameSpace,
        };
    }

    /// <summary>
    /// Find and validate the controller name from the json.
    /// Throws an exception if invalid.
    /// </summary>
    private static string GetControllerName(string fileName, 
        bool hasSyntaxTree, string nameSpace, Dictionary<string, object?> jsonDocument)
    {
        // Verify .cs file exists
        if (!hasSyntaxTree)
            throw new Exception($"No '.cs' file found, expecting a file named '{fileName}.cs' in the project.");

        // Verify it has a namespace
        if (nameSpace == "")
            throw new Exception($"No namespace declaration found in {fileName}.cs");

        // Retrieve and validate the controller type name
        if (!jsonDocument.TryGetValue("Controller", out var controllerJsonObject))
            throw new Exception("Top level JSON must contain a 'Controller' key");

        if (controllerJsonObject is not string controllerClassName || string.IsNullOrWhiteSpace(controllerClassName))
            throw new Exception("The JSON 'Controller' key must be a non-empty string");

        if (!nameSpace.StartsWith("ZurfurGui.") && !controllerClassName.Contains('.'))
            throw new Exception($"The JSON 'Controller' key must contain a dot, e.g. 'MyLibrary.MyControl'");

        if (!nameSpace.StartsWith("ZurfurGui.") && controllerClassName != $"{nameSpace}.{fileName}")
            throw new Exception($"The JSON 'Controller' key must match the full class name '{nameSpace}.{fileName}', but is '{controllerClassName}' instead");

        return controllerClassName;
    }

    static string GetStyleName(string fileName, Dictionary<string, object?> jsonDocument)
    {
        // Retrieve and validate the style name
        if (!jsonDocument.TryGetValue("Name", out var styleJsonObject))
            throw new Exception("Top level JSON must contain a 'Style' key");
        if (styleJsonObject is not string styleName || string.IsNullOrWhiteSpace(styleName))
            throw new Exception("The JSON 'Name' key must be a non-empty string");
        if (styleName != fileName)
            throw new Exception($"The JSON 'Name' key must match the file name '{fileName}', but is '{styleName}' instead");
        return styleName;
    }

    static void GenerateControllerClasses(SourceProductionContext spc, ImmutableArray<FileInfo> zuiData)
    {
        foreach (var data in zuiData)
        {
            // Should have valid data here
            var errorLocation = Location.Create(data?.Path ?? "(unknown path)", new(), new());
            if (data == null)
            {
                var diagnostic = GetDiagnostic(errorLocation, "ZUI003", "Internal Error",
                    $"An internal error occurred during code generation for '{data?.Path}'");
                spc.ReportDiagnostic(diagnostic);
                continue;
            }

            // Report any diagnostics collected during data gathering
            if (data.Diagnostic != null)
                spc.ReportDiagnostic(data.Diagnostic);

            // Retrieve controller type name
            // NOTE: We will generate code even if controllerName is invalid.
            //       This helps to avoid cascading errors when compiling user code.
            string controllerName = "";
            if (data.Diagnostic == null)
                controllerName = data.JsonName;
            if (controllerName == "")
                controllerName = data.NamespaceFileName;
            if (data.Namespace == "")
                continue; // But when namespace is invalid so is the generated code, so skip it entirely

            try
            {
                // Generate the source code for the control
                var source = GenerateControllerClassSource(data, controllerName);
                spc.AddSource($"{data.FileName}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var diagnostic = GetDiagnostic(errorLocation, "ZUI004", "ZUI Code Generation Error",
                    $"Error while generating code from '{Path.GetFileName(data.Path)}': {ex.Message}");
                spc.ReportDiagnostic(diagnostic);
            }
        }
    }


    private static string GenerateControllerClassSource(FileInfo data, string controllerName)
    {
        // Serialize JSON and escape double quotes for verbatim string
        var zuiJsonContent = Json.Serialize(data.JsonDocument)
            .Replace("\"", "\"\"");

        // Create named controls variables
        var namedControls = FindNamedControls(data.JsonDocument).OrderBy(c => c.ControlName);
        var namedControlsCode = string.Join("\r\n", namedControls.Select(a =>
        {
            var qualifier = a.ControlName.StartsWith("_") ? "private" : "public ";
            var varType = "global::" + (a.Controller.Contains('.') ? "" : "ZurfurGui.Controls.") + a.Controller;
            return $"    {qualifier} {varType} {a.ControlName};";
        }));

        // Create the controls initialization code
        var initControlsCode = string.Join("\r\n", namedControls.Select(a =>
        {
            var qualifier = a.ControlName.StartsWith("_") ? "private" : "public ";
            var varType = "global::" + (a.Controller.Contains('.') ? "" : "ZurfurGui.Controls.") + a.Controller;
            return $"        {a.ControlName} = ({varType})View.FindByName(\"{a.ControlName}\").Controller;";
        }));

        var source = $@"
// This file is generated from '{Path.GetFileName(data.Path)}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}
namespace {data.Namespace};

public sealed partial class {data.FileName}
{{
    public global::ZurfurGui.Base.View View {{ get; private set; }}
    public string TypeName => ""{controllerName}""; 

    // Named controls (public unless name starts with '_')
{namedControlsCode}

    void InitializeControl() 
    {{
        View = new(this);
        global::ZurfurGui.Loader.Load(this, _zuiJsonContent);

        // Initialize named controls
{initControlsCode}
    }}

    static string _zuiJsonContent => @""{zuiJsonContent}"";
}}";
        return source;
    }


    static void GenerateZurfurMain(
        SourceProductionContext sourceProductionContext,
        Compilation compilation,
        ImmutableArray<FileInfo> zuiData,
        ImmutableArray<FileInfo> zssData
        )
    {
        // Build a list of generated controls (errors already reported in GenerateControllerClasses)
        var generatedControls = zuiData
            .Where(data => data.Diagnostic == null && data.JsonName != "" && data.Namespace != "");

        // Build a list of generated styles (report errors here)
        var generatedStyles = zssData
            .Where(data => 
            { 
                if (data.Diagnostic != null)
                {
                    sourceProductionContext.ReportDiagnostic(data.Diagnostic);
                    return false;
                }
                return true; 
            });

        // Skip if no controls were generated
        if (!generatedControls.Any() && !generatedStyles.Any())
            return; 

        // Find and validate ZurfurMain
        var zurfurMainClass = compilation.GetSymbolsWithName("ZurfurMain")
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(symbol => symbol.TypeKind == TypeKind.Class);

        if (zurfurMainClass == null)
        {
            // Report an error if there is no ZurfurMain class but generated controls exist
            sourceProductionContext.ReportDiagnostic(GetDiagnostic(Location.None,
                "ZUI005", "Missing ZurfurMain Class",
                $"The project '{compilation.AssemblyName}' contains generated code and needs a 'ZurfurMain' class. "
                    + "Add \"static partial class ZurfurMain\" to your project."));
            return;
        }

        // Generate source code
        var zurfurMainNamespace = zurfurMainClass.ContainingNamespace.ToDisplayString();
        var zurfurMainSource = GenerateZurfurMainSource(zurfurMainNamespace, generatedControls, generatedStyles);
        sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, Encoding.UTF8));
    }


    private static string GenerateZurfurMainSource(string nameSpace, 
        IEnumerable<FileInfo> generatedControls, IEnumerable<FileInfo> generatedStyles)
    {
        var registerControls = string.Join("\r\n", generatedControls.Select(t =>
            $"        global::ZurfurGui.Loader.RegisterControl(\"{t.JsonName}\", typeof(global::{t.NamespaceFileName}));"));
        
        var createControls = string.Join("\r\n", generatedControls.Select(t =>
            $"            _ = new global::{t.NamespaceFileName}();"));

        var registerStyles = string.Join("\r\n", generatedStyles.Select(t =>
            $"\r\n        // Register style '{t.JsonName}'\r\n"
            + $"        global::ZurfurGui.Loader.RegisterStyleSheet(@\"{Json.Serialize(t.JsonDocument).Replace("\"", "\"\"")}\");\r\n"));


        return $@"
namespace {nameSpace};

// Each project should have a user created partial class named ZurfurMain
// with a function named MainApp that calls InitializeControls.
static partial class ZurfurMain
{{
    public static bool s_create = false; // Keep AOT trimming from removing constructors

    // The user created function MainApp should call this function
    private static void InitializeControls()
    {{
        // Reister Controls
{registerControls}

        // Keep AOT trimming from removing constructors, but don't actually create them
        if (s_create)
        {{
{createControls}
        }}
{registerStyles}
    }}
}}";
    }

    private static string? GetNameSpace(SyntaxTree? csTree)
    {
        if (csTree == null)
            return null;

        var root = csTree.GetRoot();

        // Try to get the namespace from the namespace declaration
        var namespaceNode = root.DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceNode != null)
        {
            return BuildNamespaceName(namespaceNode);
        }

        // Try file-scoped namespace
        var fileScopedNs = root.DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNs != null)
        {
            return fileScopedNs.Name.ToString();
        }

        return null;

        // Helper function to recursively build the fully qualified namespace
        string BuildNamespaceName(SyntaxNode node)
        {
            if (node is NamespaceDeclarationSyntax namespaceNode)
            {
                var parentNamespace = namespaceNode.Parent is NamespaceDeclarationSyntax
                    ? BuildNamespaceName(namespaceNode.Parent)
                    : null;

                return parentNamespace == null
                    ? namespaceNode.Name.ToString()
                    : $"{parentNamespace}.{namespaceNode.Name}";
            }

            return string.Empty;
        }
    }

    private static Diagnostic GetDiagnostic(Location location, string id, string title, string messageFormat)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category: "ZurfurGuiGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
        return Diagnostic.Create(descriptor, location);
    }



    /// <summary>
    /// Recursively scan JSON control, looking for named controls.
    /// </summary>
    private static List<(string Controller, string ControlName)> FindNamedControls(Dictionary<string, object?> json)
    {
        var result = new List<(string Controller, string ControlName)>();
        ScanJson(json);
        return result;

        // Helper function to recursively scan the JSON
        void ScanJson(Dictionary<string, object?> currentJson)
        {
            // Check if the current dictionary contains both "Controller" and "(Name)"
            if (currentJson.TryGetValue("(Name)", out var controlNameObj)
                && controlNameObj is string controlName)
            {
                var controller = "Panel";
                if (currentJson.TryGetValue("Controller", out var controllerObj)
                    && controllerObj is string controllerStr)
                {
                    controller = controllerStr;
                }
                result.Add((controller, controlName));
            }

            // Recursively scan nested dictionaries or arrays
            foreach (var value in currentJson.Values)
            {
                if (value is Dictionary<string, object?> nestedDict)
                {
                    ScanJson(nestedDict);
                }
                else if (value is List<object?> nestedList)
                {
                    foreach (var item in nestedList)
                    {
                        if (item is Dictionary<string, object?> listItemDict)
                        {
                            ScanJson(listItemDict);
                        }
                    }
                }
            }
        }

    }
}