using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZuiGen;

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

            // Try to find the .cs file with the same name as the class
            var csTree = syntaxTreesArray.FirstOrDefault(tree =>
            {
                var csFileName = Path.GetFileNameWithoutExtension(tree.FilePath);
                return string.Equals(csFileName, className, StringComparison.OrdinalIgnoreCase);
            });

            if (csTree == null)
            {
                // Report a diagnostic error if no corresponding .cs file
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI001",
                    title: "Missing corresponding .cs file",
                    messageFormat: $"No corresponding .cs file found for '{Path.GetFileName(zuiPath)}'. Expected a file named '{className}.cs' in the project.",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
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
                        messageFormat: $"No namespace found in the file '{Path.GetFileName(zuiPath)}'.",
                        category: "ZurfurGuiGen",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    );
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
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
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
                return;
            }

            try
            {
                var source = GenerateSourceCode(zuiPath, className, nameSpace, zuiJsonContent);
                sourceProductionContext.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var descriptor = new DiagnosticDescriptor(
                    id: "ZUI004",
                    title: "ZUI Code Generation Error",
                    messageFormat: $"An error occurred while generating code from '{Path.GetFileName(zuiPath)}': {ex.Message}",
                    category: "ZurfurGuiGen",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                );
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
            }
        });
    }

    private static string GenerateSourceCode(string zuiPath, string className, string nameSpace, string zuiJsonContent)
    {

        zuiJsonContent = zuiJsonContent
            .Replace("\"", "\"\"").Replace(" ", "").Replace("\r", "").Replace("\n", "");


        // TBD: FAILS because we can't use System.Text.Json
        //var jsonDocument = JsonSerializer.Deserialize<JsonElement>(zuiJsonContent);

        var source = $@"
            // This file is generated from '{Path.GetFileName(zuiPath)}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            namespace {nameSpace};

            public partial class {className}
            {{
                void InitializeComponent() 
                {{
                }}

                string JsonContent => @""{zuiJsonContent}"";
                
            }}";
        return source;
    }

}