using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

[Generator]
public class GenerateZui : IIncrementalGenerator
{

    // NOTE: We can't use record class here because code generators must target netstandard2.0
    class ZuiData
    {
        public Diagnostic? Diagnostic { get; set; }
        public string ZuiPath { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string Class { get; set; } = "";
        public Dictionary<string, object?> JsonDocument { get; set; } = new();
        public string ControllerTypeName { get; set; } = "";
        public string NamespaceClass => $"{Namespace}.{Class}";
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all additional files ending with .zui.json
        var zuiJsonFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase));

        // Collect all syntax trees in the compilation
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());

        // Combine zuiJsonFiles and syntaxTrees
        var combinedJsonSyntax = zuiJsonFiles.Combine(syntaxTrees);

        // Collect data for all .zui.json files
        var collectedData = combinedJsonSyntax.Select((combinedJsonSyntax, cancellationToken) =>
        {
            var additionalText = combinedJsonSyntax.Left;
            var zuiPath = additionalText.Path;
            var errorLocationTop = Location.Create(zuiPath,
                new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
            try
            {
                var syntaxTreesArray = combinedJsonSyntax.Right;
                var fileName = Path.GetFileNameWithoutExtension(zuiPath); // removes .json
                var className = Path.GetFileNameWithoutExtension(fileName); // removes .zui

                // Try to find the .cs file with the same name as the class
                var csTree = syntaxTreesArray.FirstOrDefault(tree =>
                {
                    var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
                    return string.Equals(csFileName, fileName, StringComparison.OrdinalIgnoreCase);
                });

                if (csTree == null)
                    throw new Exception($"No '.cs' file found, expecting a file named '{fileName}.cs' in the project.");

                // Try to get the namespace from the .cs file
                var nameSpace = GetNameSpace(csTree);
                if (nameSpace == null)
                    throw new Exception("No namespace declaration found");

                // Get the contents of the .zui.json file as a string
                var zuiJsonContent = additionalText.GetText(cancellationToken)?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(zuiJsonContent))
                    throw new Exception("The .zui.json file is empty");

                // Parse the JSON content
                var jsonDocument = Json.Parse(zuiJsonContent);

                // Retrieve and validate the controller type name
                if (!jsonDocument.TryGetValue("Controller", out var controllerJsonObject))
                    throw new Exception("Top level JSON must contain a 'Controller' key");
                if (controllerJsonObject is not string controllerClassName || string.IsNullOrWhiteSpace(controllerClassName))
                    throw new Exception("The JSON 'Controller' key must be a non-empty string");
                if (!controllerClassName.Contains('.') && !nameSpace.StartsWith("ZurfurGui."))
                    throw new Exception($"The JSON 'Controller' key must contain a dot, e.g. 'MyLibrary.MyControl'");
                if (!nameSpace.StartsWith("ZurfurGui.") && controllerClassName != $"{nameSpace}.{className}")
                    throw new Exception($"The JSON 'Controller' key must match the full class name '{nameSpace}.{className}', but is '{controllerClassName}' instead");

                return new ZuiData
                {
                    ZuiPath = zuiPath,
                    Class = className,
                    Namespace = nameSpace,
                    JsonDocument = jsonDocument,
                    ControllerTypeName = controllerClassName
                };
            }
            catch (LocationException lex)
            {
                // Report a diagnostic error if there is a LocationException
                var errorLocation = Location.Create(zuiPath, new TextSpan(0, 0),
                    new LinePositionSpan(new LinePosition(lex.Line, lex.Column), new LinePosition(lex.Line, lex.Column)));
                var diagnostic = GetDiagnostic(errorLocation,
                    "ZUI004", "ZUI JSON Parsing Error",
                    $"Error while parsing JSON '{Path.GetFileName(zuiPath)}': {lex.Message}");
                return new ZuiData { Diagnostic = diagnostic };
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var diagnostic = GetDiagnostic(errorLocationTop, "ZUI005", "ZUI Code Generation Error",
                    $"Error while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}");
                return new ZuiData { Diagnostic = diagnostic };
            }
        });

        // Generate source code in the second phase
        var generatedControls = new Dictionary<string, ZuiData>();
        context.RegisterSourceOutput(collectedData.Collect(), (sourceProductionContext, collectedData) =>
        {
            // Find the generated controls
            foreach (var data in collectedData)
            {
                if (data != null && data.ControllerTypeName != null)
                    generatedControls[data.ControllerTypeName] = data;
            }

            foreach (var data in collectedData)
            {
                // Report any diagnostics collected during data gathering
                if (data != null && data.Diagnostic != null)
                {
                    // Report any diagnostics collected during data gathering
                    sourceProductionContext.ReportDiagnostic(data.Diagnostic);
                    continue;
                }

                // Nothing should be null or empty here, errors should be reported above
                var errorLocation = Location.Create(data?.ZuiPath??"(unknown path)", new(), new());
                if (data == null || data.ZuiPath == "" || data.Namespace == "" 
                    || data.Class == "" || data.ControllerTypeName == "")
                { 
                    var diagnostic = GetDiagnostic(errorLocation, "ZUI006", "Internal Error",
                        $"An internal error occurred during code generation for '{data?.ZuiPath}'");
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                    continue;
                }

                try
                {
                    // Generate the source code for the control
                    var source = GenerateControllerClass(data);
                    sourceProductionContext.AddSource($"{data.Class}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    // Report a diagnostic error if there is an exception during code generation
                    var diagnostic = GetDiagnostic(errorLocation, "ZUI007", "ZUI Code Generation Error",
                        $"Error while generating code from '{Path.GetFileName(data.ZuiPath)}': {ex.Message}");
                    sourceProductionContext.ReportDiagnostic(diagnostic);
                }
            }
        });

        // Generate the ZurfurMain partial class in each project
        context.RegisterSourceOutput(context.CompilationProvider, (sourceProductionContext, compilation) =>
        {
            if (!generatedControls.Any())
                return; // Skip if no controls were generated

            var zurfurMainClass = compilation.GetSymbolsWithName("ZurfurMain")
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(symbol => symbol.TypeKind == TypeKind.Class);

            if (zurfurMainClass == null)
            {
                // Report an error if there is no ZurfurMain class but generated controls exist
                sourceProductionContext.ReportDiagnostic(GetDiagnostic(Location.None,
                    "ZUI008", "Missing ZurfurMain Class",
                    $"The project '{compilation.AssemblyName}' contains generated code and needs a 'ZurfurMain' class. "
                        + "Add \"static partial class ZurfurMain\" to your project."));
                return;
            }

            var zurfurMainNamespace = zurfurMainClass.ContainingNamespace.ToDisplayString();
            var zurfurMainSource = GenerateZurfurMainClass(zurfurMainNamespace, generatedControls);
            sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, Encoding.UTF8));
        });

    }


    private static string? GetNameSpace(SyntaxTree csTree)
    {
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

    private static string GenerateZurfurMainClass(string nameSpace, Dictionary<string, ZuiData> generatedControls)
    {
        var registerCalls = string.Join("\r\n", generatedControls.Values.Select(t =>
            $"        global::ZurfurGui.Loader.RegisterControl(\"{t.ControllerTypeName}\", typeof(global::{t.NamespaceClass}));"));
        var createControls = string.Join("\r\n", generatedControls.Values.Select(t =>
            $"            _ = new global::{t.NamespaceClass}();"));

        return $@"
namespace {nameSpace};
static partial class ZurfurMain
{{
    public static bool s_create = false; // Keep AOT trimming from removing constructors

    public static void InitializeControls()
    {{
{registerCalls}

        // Keep AOT trimming from removing constructors, but don't actually create them
        if (s_create)
        {{
{createControls}
        }}
    }}
}}";
    }

    private static string GenerateControllerClass(ZuiData data)
    {
        // Remove whitespace and add escapes to embed the JSON in the source code
        var zuiJsonContent = Json.Serialize(data.JsonDocument)
            .Replace("\"", "\"\"").Replace("\r", "").Replace("\n", "");

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
// This file is generated from '{Path.GetFileName(data.ZuiPath)}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}
namespace {data.Namespace};

public sealed partial class {data.Class}
{{
    public global::ZurfurGui.Base.View View {{ get; private set; }}
    public string TypeName => ""{data.ControllerTypeName}""; 

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