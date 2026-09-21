using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;


[Generator]
public class GenerateZui : IIncrementalGenerator
{


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxTrees = context.CompilationProvider.Select((compilation, _) => compilation.SyntaxTrees.ToArray());


        // Collect zuiData for all ZUI JSON5 files
        var zuiData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zui.json", StringComparison.OrdinalIgnoreCase)
                || file.Path.EndsWith(".zui.json5", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
        {
            var text = combined.Left;
            var syntax = combined.Right;
            return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
        });

        // Collect zthData for all ZTH JSON5 files (theme tokens)
        var zthData = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".zth.json", StringComparison.OrdinalIgnoreCase)
                || file.Path.EndsWith(".zth.json5", StringComparison.OrdinalIgnoreCase))
            .Combine(syntaxTrees)
            .Select((combined, cancellationToken) =>
            {
                var text = combined.Left;
                var syntax = combined.Right;
                return ZuiInput.CollectJsonFiles(text, syntax, cancellationToken);
            });

        // Generate controller classes.
        context.RegisterSourceOutput(zuiData.Collect(),
            (spc, data) => GenerateControllerClasses(spc, data));

        // Generate the ZurfurMain partial class in each project
        context.RegisterSourceOutput(zuiData.Collect().Combine(zthData.Collect())
            .Combine(context.CompilationProvider), (sourceProductionContext, quad) =>
        {
            var collectedZuiData = quad.Left.Left;
            var collectedZthData = quad.Left.Right;
            var compilation = quad.Right;
            ZuiEmitMain.GenerateZurfurMain(sourceProductionContext, compilation, collectedZuiData, collectedZthData);
        });
    }

    /// <summary>
    /// Collect FileInfo from a .json file.
    /// If a corresponding .cs file exists, collect the namespace from it.
    /// </summary>
    static void GenerateControllerClasses(SourceProductionContext spc, ImmutableArray<ZuiFileInfo> zuiData)
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
                var source = ZuiEmitController.GenerateControllerClassSource(data);
                if (source != "")
                    spc.AddSource($"{data.FileName}.Control.g.cs", SourceText.From(source, Encoding.UTF8));

            var dataImplSource = ZuiEmitData.GenerateDataImplementationSource(data);
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

}
