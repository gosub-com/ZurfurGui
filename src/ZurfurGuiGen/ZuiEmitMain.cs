using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;

internal static class ZuiEmitMain
{
    // Matches a closed generic controller usage e.g. "ComboBox<ComboBoxItemText>"
    static readonly Regex s_closedGenericRegex = new Regex(@"^(\w+)<(\w+)>$", RegexOptions.Compiled);
internal static void GenerateZurfurMain(
    SourceProductionContext sourceProductionContext,
    Compilation compilation,
    ImmutableArray<ZuiFileInfo> zuiData,
    ImmutableArray<ZuiFileInfo> zthData
    )
    {
        // Build a list of generated controls (errors already reported in GenerateControllerClasses)
        var generatedControls = zuiData
            .Where(data => data.Diagnostic == null && data.ControllerName != "");

        // Build a list of generated themes (report errors here)
        var generatedThemes = zthData
            .Where(data => 
            {
                if (data.Diagnostic != null)
                {
                    sourceProductionContext.ReportDiagnostic(data.Diagnostic);
                    return false;
                }
                return true;
            });

        // Skip if no controls, styles, or themes were generated
        if (!generatedControls.Any() && !generatedThemes.Any())
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
        var zurfurMainSource = GenerateZurfurMainSource(sourceProductionContext, compilation,
            zurfurMainNamespace, generatedControls, generatedThemes);
        sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, System.Text.Encoding.UTF8));
    }

internal static string GenerateZurfurMainSource(SourceProductionContext sourceProductionContext,
    Compilation compilation, string zurfurMainNamespace,
    IEnumerable<ZuiFileInfo> generatedControls, IEnumerable<ZuiFileInfo> generatedThemes)
    {
        var controlList = generatedControls.ToList();

        // typeof() for open generics requires the <> syntax; non-generics use the plain name.
        // For generic controls, also emit a RunClassConstructor for the generated non-generic
        // companion static class (e.g. "ComboBox"), which may contain hand-written PropertyKey
        // fields. Running only the open generic (ComboBox<>) does not reliably trigger the
        // companion's type initializer, so those keys would remain unregistered when style sheets
        // are parsed — causing "Unknown property name" exceptions at runtime.
        var runStaticConstructors = string.Join("\r\n", controlList.SelectMany(t =>
        {
            var typeName = t.TypeParam != ""
                ? $"global::{t.NamespaceFileName}<>"
                : $"global::{t.NamespaceFileName}";
            var lines = new List<string>
            {
                $"        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({typeName}).TypeHandle);"
            };
            // Run the generated non-generic companion's static constructor explicitly so any
            // hand-authored PropertyKey fields are registered before style sheets are parsed.
            if (t.TypeParam != "")
            {
                var companionName = $"global::{t.Namespace}.{t.ControllerName}";
                lines.Add($"        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({companionName}).TypeHandle);");
            }
            return lines;
        }));

        // Register non-generic controls by name, type, and factory.
        var registerControls = string.Join("\r\n", controlList
            .Where(t => t.TypeParam == "")
            .Select(t => $"        global::ZurfurGui.Generated.ZuiRuntime.RegisterControl(\"{t.NamespaceFileName}\", typeof(global::{t.NamespaceFileName}), () => new global::{t.NamespaceFileName}());"));

        var registerDataControllers = string.Join("\r\n", controlList
            .Where(t => t.TypeParam == "" && t.Bindings.Any(b => !b.BindType.IsViewOnly()))
            .Select(t => $"        global::ZurfurGui.Generated.ZuiRuntime.RegisterDataController(typeof(global::{t.NamespaceFileName}Data), itemData => {{ var controller = new global::{t.NamespaceFileName}(); controller.DataContext = (global::{t.NamespaceFileName}Data)itemData; return controller; }});"));

        // Scan all JSON documents recursively for closed generic controller usages
        // (e.g. "$controller": "ComboBox<ComboBoxItemText>" at a usage site).
        // Open generic definitions (e.g. "ComboBox<Item>") are skipped.
        // The dictionary maps the literal usage string (used directly as the Loader key, with no
        // namespace prefix) -> (base control bare name, item controller bare name). Both names are
        // resolved to their real namespaces here, at generation time, so the generated code names
        // the closed generic type directly (e.g. new ComboBox<ComboBoxItemTextData>()). The
        // compiler and AOT/trimming tools can then see the type and keep its constructor, which
        // is not possible when the closed type is only created with reflection at runtime.
        var closedGenerics = new SortedDictionary<string, (string BaseName, string ItemName)>();
        foreach (var fileInfo in controlList)
        {
            CollectClosedGenerics(fileInfo.JsonDocument, fileInfo, closedGenerics);
        }

        var resolver = new ZuiTypeResolver(controlList, compilation);
        var registerClosedGenerics = string.Join("\r\n", closedGenerics.Select(kvp =>
        {
            var loaderKey = kvp.Key;            // e.g. "ComboBox<ComboBoxItemText>", used as-is
            var baseType = resolver.Resolve(kvp.Value.BaseName, 1, out var baseError);
            var itemType = resolver.Resolve(kvp.Value.ItemName, 0, out var itemError);
            if (baseType == null || itemType == null)
            {
                sourceProductionContext.ReportDiagnostic(ZuiDiagnostics.GetDiagnostic(Location.None,
                    "ZUI006", "Unresolved Generic Control",
                    $"Cannot generate closed generic control '{loaderKey}'. {baseError}{itemError}"));
                return "";
            }

            // The generated data class of the item controller, e.g. ComboBoxItemText -> ComboBoxItemTextData
            var closedType = $"global::{baseType}<global::{itemType}Data>";

            // Closed generic static fields are per-closed-type, so the static constructor is run
            // for this specific closed type before its PropertyKey fields are used (e.g. by themes).
            return $"        // Closed generic: registered from usage site, not from the generic definition.\r\n"
                + $"        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({closedType}).TypeHandle);\r\n"
                + $"        global::ZurfurGui.Generated.ZuiRuntime.RegisterControl(\"{loaderKey}\", typeof({closedType}), () => new {closedType}());";
        }).Where(s => s != ""));

        var registerThemes = string.Join("\r\n", generatedThemes.Select(t =>
        {
            Json.RemoveKeys(t.JsonDocument, new List<string> { "#comment", "$namespace", "$use", "$data" });
            return $"\r\n        // Register theme '{t.ControllerName}'\r\n"
                + $"        global::ZurfurGui.Styles.ThemeManager.RegisterTheme(@\"{Json.Serialize(t.JsonDocument).Replace("\"", "\"\"")}\");\r\n";
        }));

        var sb = new StringBuilder();
        sb.Append("#pragma warning disable CS1591 // Missing XML comment "
            + "for publicly visible type or member\r\n");
        sb.Append($"namespace {zurfurMainNamespace};\r\n\r\n");
        sb.Append("// Each project should have a user created partial class named ZurfurMain\r\n");
        sb.Append("// with a function named MainApp that calls InitializeControls.\r\n");
        sb.Append("static partial class ZurfurMain\r\n{\r\n");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "// The user created function MainApp should call this function");
        sb.AppendIndentedLine(1, "public static void InitializeControls()");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "// Run static constructors to register control properties");
        if (!string.IsNullOrWhiteSpace(runStaticConstructors))
            sb.Append(runStaticConstructors).Append("\r\n");
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Register non-generic controls");
        if (!string.IsNullOrWhiteSpace(registerControls))
            sb.Append(registerControls).Append("\r\n");
        if (!string.IsNullOrWhiteSpace(registerDataControllers))
        {
            sb.AppendIndentedLine(2, "// Register concrete data controllers");
            sb.Append(registerDataControllers).Append("\r\n");
        }
        sb.Append("\r\n");
        if (!string.IsNullOrWhiteSpace(registerClosedGenerics))
        {
            sb.AppendIndentedLine(2, "// Register closed generic controls (discovered from usage sites)");
            sb.Append(registerClosedGenerics).Append("\r\n\r\n");
        }
        if (!string.IsNullOrWhiteSpace(registerThemes))
            sb.Append(registerThemes);
        sb.AppendIndentedLine(1, "}");
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// Recursively scan a JSON document for $controller values that are closed generics
    /// (e.g. "$controller": "ComboBox&lt;ComboBoxItemText&gt;" at a usage site).
    /// Skips open generic definitions where the type argument matches the source file's own TypeParam.
    /// The dictionary key is the literal usage string (the Loader lookup key, used verbatim) and the
    /// value is the (base control, item controller) bare class names. The caller resolves both
    /// names to their namespaces with ZuiTypeResolver.
    /// </summary>
    static void CollectClosedGenerics(Dictionary<string, object?> json,
        ZuiFileInfo sourceFile, SortedDictionary<string, (string BaseName, string ItemName)> result)
    {
        foreach (var kvp in json)
        {
            if (kvp.Key == "$controller" && kvp.Value is string controllerValue)
            {
                var match = s_closedGenericRegex.Match(controllerValue);
                if (match.Success)
                {
                    var baseName = match.Groups[1].Value;   // e.g. "ComboBox"
                    var typeArg = match.Groups[2].Value;    // e.g. "ComboBoxItemText"
                    // Skip the open generic definition's own controller value
                    if (typeArg != sourceFile.TypeParam && !result.ContainsKey(controllerValue))
                        result[controllerValue] = (baseName, typeArg);
                }
            }

            if (kvp.Value is Dictionary<string, object?> nested)
                CollectClosedGenerics(nested, sourceFile, result);

            if (kvp.Value is List<object?> list)
                foreach (var item in list)
                    if (item is Dictionary<string, object?> listItem)
                        CollectClosedGenerics(listItem, sourceFile, result);
        }
    }
}
