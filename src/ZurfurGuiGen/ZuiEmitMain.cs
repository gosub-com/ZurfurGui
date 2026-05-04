using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitMain
{
    internal static void GenerateZurfurMain(
        SourceProductionContext sourceProductionContext,
        Compilation compilation,
        ImmutableArray<ZuiTypes.FileInfo> zuiData,
        ImmutableArray<ZuiTypes.FileInfo> zssData
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
        var zurfurMainSource = GenerateZurfurMainSource(zurfurMainNamespace, generatedControls, generatedStyles);
        sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, System.Text.Encoding.UTF8));
    }

    internal static string GenerateZurfurMainSource(string zurfurMainNamespace,
        IEnumerable<ZuiTypes.FileInfo> generatedControls, IEnumerable<ZuiTypes.FileInfo> generatedStyles)
    {
        var runStaticConstructors = string.Join("\r\n", generatedControls.Select(t =>
            $"        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(global::{t.NamespaceFileName}).TypeHandle);"));

        var registerControls = string.Join("\r\n", generatedControls.Select(t =>
            $"        global::ZurfurGui.Loader.RegisterControl(\"{t.NamespaceFileName}\", typeof(global::{t.NamespaceFileName}));"));

        var createControls = string.Join("\r\n", generatedControls.Select(t =>
            $"            _ = new global::{t.NamespaceFileName}();"));

        var registerStyles = string.Join("\r\n", generatedStyles.Select(t =>
            $"\r\n        // Register style '{t.ControllerName}'\r\n"
            + $"        global::ZurfurGui.Loader.RegisterStyleSheet(@\"{Json.Serialize(t.JsonDocument).Replace("\"", "\"\"")}\");\r\n"));

        var sb = new StringBuilder();
        sb.Append($"namespace {zurfurMainNamespace};\r\n\r\n");
        sb.Append("// Each project should have a user created partial class named ZurfurMain\r\n");
        sb.Append("// with a function named MainApp that calls InitializeControls.\r\n");
        sb.Append("static partial class ZurfurMain\r\n{\r\n");
        sb.AppendIndentedLine(1, "public static bool s_create = false; // Keep AOT trimming from removing constructors");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "// The user created function MainApp should call this function");
        sb.AppendIndentedLine(1, "private static void InitializeControls()");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "// Run static constructors to register control properties");
        if (!string.IsNullOrWhiteSpace(runStaticConstructors))
            sb.Append(runStaticConstructors).Append("\r\n");
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Reister Controls");
        if (!string.IsNullOrWhiteSpace(registerControls))
            sb.Append(registerControls).Append("\r\n");
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Keep AOT trimming from removing constructors, but don't actually create them");
        sb.AppendIndentedLine(2, "if (s_create)");
        sb.AppendIndentedLine(2, "{");
        if (!string.IsNullOrWhiteSpace(createControls))
            sb.Append(createControls).Append("\r\n");
        sb.AppendIndentedLine(2, "}");
        if (!string.IsNullOrWhiteSpace(registerStyles))
            sb.Append(registerStyles);
        sb.AppendIndentedLine(1, "}");
        sb.Append("}");
        return sb.ToString();
    }
}
