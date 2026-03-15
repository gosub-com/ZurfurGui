using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

using FileInfo = ZurfurGuiGen.ZuiTypes.FileInfo;

[Generator]
public class GenerateZui : IIncrementalGenerator
{


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());


        // Collect zuiData for all ZUI.JSON files
        var zuiData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
        {
            var text = combined.Left;
            var syntax = combined.Right;
            return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
        });

        // Collect zssData for all ZSS.JSON files
        var zssData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zss.json", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
            {
                var text = combined.Left;
                var syntax = combined.Right;
                return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
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
    static void GenerateControllerClasses(SourceProductionContext spc, ImmutableArray<FileInfo> zuiData)
    {
        foreach (var data in zuiData)
        {
            // Should have valid data here
            var errorLocation = Location.Create(data?.Path ?? "(unknown path)", new(), new());
            if (data == null)
            {
                var diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocation, "ZUI003", "Internal Error",
                    $"An internal error occurred during code generation for '{data?.Path}'");
                spc.ReportDiagnostic(diagnostic);
                continue;
            }

            // Report any diagnostics collected during data gathering
            if (data.Diagnostic != null)
            {
                spc.ReportDiagnostic(data.Diagnostic);
                continue;
            }

            try
            {
                // Generate the source code for the control
                var source = ZuiEmit.GenerateControllerClassSource(data);
                if (source != "")
                    spc.AddSource($"{data.FileName}.Control.g.cs", SourceText.From(source, Encoding.UTF8));

                // Generate the source code for the data contract (interface)
                var dataContractSource = ZuiEmit.GenerateDataContractInterfaceSource(data);
                if (dataContractSource != "")
                    spc.AddSource($"{data.FileName}.DataContract.g.cs", SourceText.From(dataContractSource, Encoding.UTF8));

                // Generate the source code for the data implementation (partial if *Data.cs exists)
                var dataImplSource = ZuiEmit.GenerateDataImplementationSource(data);
                if (dataImplSource != "")
                    spc.AddSource($"{data.FileName}.Data.g.cs", SourceText.From(dataImplSource, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                // Report a diagnostic error if there is an exception during code generation
                var diagnostic = ZuiDiagnostics.GetDiagnostic(errorLocation, "ZUI004", "ZUI Code Generation Error",
                    $"Error while generating code from '{Path.GetFileName(data.Path)}': {ex.Message}");
                spc.ReportDiagnostic(diagnostic);
            }
        }

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
            .Where(data => data.Diagnostic == null && data.ControllerName != "");

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
            sourceProductionContext.ReportDiagnostic(ZuiDiagnostics.GetDiagnostic(Location.None,
                "ZUI005", "Missing ZurfurMain Class",
                $"The project '{compilation.AssemblyName}' contains generated code and needs a 'ZurfurMain' class. "
                    + "Add \"static partial class ZurfurMain\" to your project."));
            return;
        }

        // Generate source code
        var zurfurMainNamespace = zurfurMainClass.ContainingNamespace.ToDisplayString();
        var zurfurMainSource = ZuiEmit.GenerateZurfurMainSource(zurfurMainNamespace, generatedControls, generatedStyles);
        sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, Encoding.UTF8));
    }

}