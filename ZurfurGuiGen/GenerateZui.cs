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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all additional files ending with .zui.json
        var zuiJsonFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase));

        // Collect all syntax trees in the compilation
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());

        // Combine zuiJsonFiles and syntaxTrees
        var combinedJsonSyntax = zuiJsonFiles.Combine(syntaxTrees);

        // Track generated controls
        var generatedControls = new List<(string ControllerTypeName, string ClassName)>();

        context.RegisterSourceOutput(combinedJsonSyntax, (sourceProductionContext, combinedJsonSyntax) =>
        {
            var additionalText = combinedJsonSyntax.Left;
            var syntaxTreesArray = combinedJsonSyntax.Right;

            var zuiPath = additionalText.Path;
            var fileName = Path.GetFileNameWithoutExtension(zuiPath); // removes .json
            var className = Path.GetFileNameWithoutExtension(fileName); // removes .zui
            var errorLocationTop = Location.Create(zuiPath,
                new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));

            // Try to find the .cs file with the same name as the class
            var csTree = syntaxTreesArray.FirstOrDefault(tree =>
            {
                var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
                return string.Equals(csFileName, fileName, StringComparison.OrdinalIgnoreCase);
            });

            // Location of error when the line number isn't known 
            if (csTree == null)
            {
                // Report a diagnostic error if no corresponding .cs file
                ReportDiagnostic(
                    sourceProductionContext, errorLocationTop,
                    "ZUI001", "Missing corresponding .cs file",
                    $"No corresponding .cs file found for '{Path.GetFileName(zuiPath)}'. Expected a file named '{fileName}.cs' in the project.");
                return;
            }
            // Try to get the namespace from the .cs file
            var root = csTree.GetRoot();
            var namespaceNode = root.DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            string nameSpace;
            if (namespaceNode != null)
            {
                // Try to get the namespace from the namespace declaration
                nameSpace = namespaceNode.Name.ToString();
            }
            else
            {
                // Try file-scoped namespace
                var fileScopedNs = root.DescendantNodes()
                    .OfType<FileScopedNamespaceDeclarationSyntax>()
                    .FirstOrDefault();
                if (fileScopedNs != null)
                {
                    nameSpace = fileScopedNs.Name.ToString();
                }
                else
                {
                    // Report a diagnostic error if no corresponding .cs file
                    ReportDiagnostic(
                        sourceProductionContext, errorLocationTop,
                        "ZUI002", "Missing namespace",
                        $"No namespace found in the .cs file for '{Path.GetFileName(zuiPath)}'.");
                    return;
                }
            }

            // Get the contents of the .zui.json file as a string
            var zuiJsonContent = additionalText.GetText(sourceProductionContext.CancellationToken)?.ToString() ?? "";
            if (zuiJsonContent == "")
            {
                // Report a diagnostic error if the .zui.json file is empty
                ReportDiagnostic(
                    sourceProductionContext, errorLocationTop,
                    "ZUI003", "Empty ZUI JSON file",
                    $"The ZUI JSON file '{Path.GetFileName(zuiPath)}' is empty.");
                return;
            }

            try
            {
                // NOTE: Generators can't use System.Text.Json
                var jsonDocument = Json.Parse(zuiJsonContent);

                // Remove whitespace and add escapes to embed the JSON in the source code
                zuiJsonContent = Json.Serialize(jsonDocument); // Remove whitespace
                zuiJsonContent = zuiJsonContent.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", "");

                // Retrieve and validate the controller type name
                if (!jsonDocument.TryGetValue("Controller", out var controllerJsonObject))
                    throw new Exception("Top level JSON must contain a 'Controller' key");
                if (controllerJsonObject is not string controllerTypeName || controllerTypeName == "")
                    throw new Exception("The JSON 'Controller' key must be a non-empty string");
                if (!controllerTypeName.Contains('.') && !nameSpace.StartsWith("ZurfurGui."))
                    throw new Exception($"The JSON 'Controller' key must contain a dot, e.g. 'MyLibrary.MyControl'");
                var controllerTypeNameEnd = controllerTypeName.Split('.').Last();
                var classNameEnd = className.Split('.').Last();
                if (controllerTypeNameEnd != classNameEnd)
                    throw new Exception($"The JSON 'Controller' key '{controllerTypeNameEnd}' must match the class name '{classNameEnd}'");

                // Track the generated control
                generatedControls.Add((controllerTypeName, $"{nameSpace}.{className}"));

                // Generate the source code for the control
                var source = GenerateSourceCode(zuiPath, className, nameSpace, controllerTypeName, zuiJsonContent);
                sourceProductionContext.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));

            }
            catch (LocationException lex)
            {
                // Report a diagnostic error if there is a LocationException
                var errorLocation = Location.Create(zuiPath, new TextSpan(0, 0),
                    new LinePositionSpan(new LinePosition(lex.Line, lex.Column), new LinePosition(lex.Line, lex.Column)));
                ReportDiagnostic(
                    sourceProductionContext, errorLocation,
                    "ZUI005", "ZUI JSON Parsing Error",
                    $"Error while parsing '{Path.GetFileName(zuiPath)}': {lex.Message}");
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                ReportDiagnostic(
                    sourceProductionContext, errorLocationTop,
                    "ZUI004", "ZUI Code Generation Error",
                    $"Error while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}");
            }
        });

        // Generate the ZurfurControls partial class in each project
        context.RegisterSourceOutput(context.CompilationProvider, (sourceProductionContext, compilation) =>
        {
            var zurfurControlsClass = FindZurfurControlsClass(compilation);
            if (zurfurControlsClass == null && generatedControls.Any())
            {
                // Get the project/assembly name
                var projectName = compilation.AssemblyName;

                // Report an error if there is no ZurfurControls class but generated controls exist
                ReportDiagnostic(
                    sourceProductionContext, Location.None,
                    "ZUI007", "Missing ZurfurControls Class",
                    $"The project '{projectName}' contains generated code and needs a 'ZurfurControls' class. Add \"public static partial class ZurfurControls {{}}\" to your project.");
                return;
            }

            if (zurfurControlsClass != null)
            {
                var zurfurControlsNamespace = zurfurControlsClass.ContainingNamespace.ToDisplayString();
                var zurfurControlsSource = GenerateZurfurControlsPartialClass(zurfurControlsNamespace, generatedControls);
                sourceProductionContext.AddSource("ZurfurControls.g.cs", SourceText.From(zurfurControlsSource, Encoding.UTF8));
            }
        });
    }

    private static void ReportDiagnostic(SourceProductionContext context, Location location,
        string id, string title, string messageFormat)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category: "ZurfurGuiGen",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
    }

    private static INamedTypeSymbol? FindZurfurControlsClass(Compilation compilation)
    {
        return compilation.GetSymbolsWithName("ZurfurControls")
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(symbol => symbol.TypeKind == TypeKind.Class);
    }

    private static string GenerateZurfurControlsPartialClass(string nameSpace, List<(string ControllerTypeName, string ClassName)> generatedControls)
    {
        var registerCalls = string.Join("\r\n", generatedControls.Select(t =>
            $"        global::ZurfurGui.Loader.RegisterControl(\"{t.ControllerTypeName}\", typeof(global::{t.ClassName}));"));

        var createControls = string.Join("\r\n", generatedControls.Select(t =>
            $"            _ = new global::{t.ClassName}();"));


        return $@"
namespace {nameSpace};
public static partial class ZurfurControls
{{
    public static bool s_create = false; // Keep AOT trimming from removing constructors

    public static void Register()
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

    private static string GenerateSourceCode(string zuiPath, 
        string className, string nameSpace, string controllerTypeName, string zuiJsonContent)
    {
        var source = $@"
// This file is generated from '{Path.GetFileName(zuiPath)}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}
namespace {nameSpace};

public sealed partial class {className}
{{
    void InitializeControl() 
    {{
        View = new(this);
        global::ZurfurGui.Loader.Load(this, _zuiJsonContent);
    }}
    public global::ZurfurGui.Base.View View {{ get; private set; }}
    public string TypeName => ""{controllerTypeName}""; 

    static string _zuiJsonContent => @""{zuiJsonContent}"";
}}";
        return source;
    }

}