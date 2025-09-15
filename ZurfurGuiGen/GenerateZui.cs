using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection.Metadata;

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
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI001",
                    title: "Missing corresponding .cs file",
                    messageFormat: $"No corresponding .cs file found for '{Path.GetFileName(zuiPath)}'. Expected a file named '{fileName}.cs' in the project.",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, errorLocationTop));
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
                    var descriptor = new DiagnosticDescriptor(
                        id: "ZUI002",
                        title: "Missing namespace",
                        messageFormat: $"No namespace found in the .cs file for '{Path.GetFileName(zuiPath)}'.",
                        category: "ZurfurGuiGen",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    );
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, errorLocationTop));
                    return;
                }
            }

            // Get the contents of the .zui.json file as a string
            var zuiJsonContent = additionalText.GetText(sourceProductionContext.CancellationToken)?.ToString() ?? "";
            if (zuiJsonContent == "")
            {
                // Report a diagnostic error if the .zui.json file is empty
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI003",
                    title: "Empty ZUI JSON file",
                    messageFormat: $"The ZUI JSON file '{Path.GetFileName(zuiPath)}' is empty.",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, errorLocationTop));
                return;
            }

            try
            {
                var source = GenerateSourceCode(zuiPath, className, nameSpace, zuiJsonContent);
                sourceProductionContext.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (LocationException lex)
            {
                // Report a diagnostic error if there is a LocationException
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI005",
                    title: "ZUI JSON Parsing Error",
                    messageFormat: $"Error while parsing '{Path.GetFileName(zuiPath)}': {lex.Message}",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                var errorLocation = Location.Create(zuiPath, new TextSpan(0, 0), 
                    new LinePositionSpan(new LinePosition(lex.Line, lex.Column), new LinePosition(lex.Line, lex.Column)));
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, errorLocation));
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI004",
                    title: "ZUI Code Generation Error",
                    messageFormat: $"Error while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, errorLocationTop));
            }
        });
    }

    private static string GenerateSourceCode(string zuiPath, string className, string nameSpace, string zuiJsonContent)
    {
        // NOTE: Generators can't use System.Text.Json, yet...
        var jsonDocument = Json.ParseJson(zuiJsonContent);

        // TBD: Remove insignificant whitespace from zuiJsonContent
        zuiJsonContent = zuiJsonContent.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", "");

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
                public global::ZurfurGui.Base.Properties _zuiProperties;
                public global::ZurfurGui.Controls.View View {{ get; private set; }}
                public string TypeName => ""{controllerTypeName}""; 
                public override string ToString() => View.ToString();

                static string _zuiJsonContent => @""{zuiJsonContent}"";
            }}";
        return source;
    }

}