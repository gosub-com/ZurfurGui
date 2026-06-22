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
    // Matches a closed generic controller usage e.g. "ComboBox<IComboBoxItemTextData>"
    static readonly Regex s_closedGenericRegex = new Regex(@"^(\w+)<(\w+)>$", RegexOptions.Compiled);
internal static void GenerateZurfurMain(
    SourceProductionContext sourceProductionContext,
    Compilation compilation,
    ImmutableArray<ZuiFileInfo> zuiData,
    ImmutableArray<ZuiFileInfo> zssData,
    ImmutableArray<ZuiFileInfo> zthData
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
        if (!generatedControls.Any() && !generatedStyles.Any() && !generatedThemes.Any())
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
        var zurfurMainSource = GenerateZurfurMainSource(zurfurMainNamespace, generatedControls, generatedStyles, generatedThemes);
        sourceProductionContext.AddSource("ZurfurMain.g.cs", SourceText.From(zurfurMainSource, System.Text.Encoding.UTF8));
    }

internal static string GenerateZurfurMainSource(string zurfurMainNamespace,
    IEnumerable<ZuiFileInfo> generatedControls, IEnumerable<ZuiFileInfo> generatedStyles, IEnumerable<ZuiFileInfo> generatedThemes)
    {
        var controlList = generatedControls.ToList();

        // typeof() for open generics requires the <> syntax; non-generics use the plain name.
        // For generic controls, also emit a RunClassConstructor for the non-generic companion
        // static class (e.g. "ComboBox") that holds hand-written PropertyKey fields such as
        // ScrimColor.  Running only the open generic (ComboBox<>) does not reliably trigger the
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
            // Generic controls have a non-generic companion static class that may hold
            // hand-authored PropertyKey fields (e.g. ComboBox.ScrimColor).  Run its
            // static constructor explicitly so those keys are registered before any
            // style sheet is parsed.
            if (t.TypeParam != "")
            {
                var companionName = $"global::{t.Namespace}.{t.ControllerName}";
                lines.Add($"        global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({companionName}).TypeHandle);");
            }
            return lines;
        }));

        // Register non-generic controls by name, type, and factory.
        // Controls with .implements also pass their static s_implementsDataInterfaces array so
        // the loader can wire up data-controller mappings without a separate call.
        var registerControls = string.Join("\r\n", controlList
            .Where(t => t.TypeParam == "")
            .Select(t => t.Implements != ""
                ? $"        global::ZurfurGui.Loader.RegisterControl(\"{t.NamespaceFileName}\", typeof(global::{t.NamespaceFileName}), () => new global::{t.NamespaceFileName}(), global::{t.NamespaceFileName}.s_implementsDataInterfaces);"
                : $"        global::ZurfurGui.Loader.RegisterControl(\"{t.NamespaceFileName}\", typeof(global::{t.NamespaceFileName}), () => new global::{t.NamespaceFileName}());"));

        // Scan all JSON documents recursively for closed generic controller usages
        // (e.g. ".controller": "ComboBox<ComboBoxItemText>" at a usage site).
        // Open generic definitions (e.g. "ComboBox<Item>") are skipped.
        // The dictionary maps JSON name (used as Loader key) -> C# type name (for typeof/new).
        // Control names in type args are translated to their data interface names
        // (e.g. ComboBoxItemText -> IComboBoxItemTextData) using controlList.
        var closedGenerics = new SortedDictionary<string, string>();
        foreach (var fileInfo in controlList)
        {
            CollectClosedGenerics(fileInfo.JsonDocument, fileInfo, controlList, closedGenerics);
        }

        var registerClosedGenerics = closedGenerics.Count == 0 ? "" : string.Join("\r\n",
            closedGenerics.Select(kvp =>
            {
                var jsonName = kvp.Key;   // e.g. "ComboBox<ComboBoxItemText>"
                var csName = kvp.Value;   // e.g. "ComboBox<IComboBoxItemTextData>"
                // Registration key uses namespace + json name so Loader.FindControllerEntry resolves it
                var loaderKey = $"ZurfurGui.Controls.{jsonName}";
                return $"        // Closed generic: registered from usage site, not from the generic definition.\r\n"
                    + $"        global::ZurfurGui.Loader.RegisterControl(\"{loaderKey}\", typeof(global::ZurfurGui.Controls.{csName}), () => new global::ZurfurGui.Controls.{csName}());";
            }));


        var registerStyles = string.Join("\r\n", generatedStyles.Select(t =>
            $"\r\n        // Register style '{t.ControllerName}'\r\n"
            + $"        global::ZurfurGui.Styles.StyleManager.RegisterStyleSheet(@\"{Json.Serialize(t.JsonDocument).Replace("\"", "\"\"")}\");\r\n"));

        var registerThemes = string.Join("\r\n", generatedThemes.Select(t =>
            $"\r\n        // Register theme '{t.ControllerName}'\r\n"
            + $"        global::ZurfurGui.Styles.ThemeManager.RegisterTheme(@\"{Json.Serialize(t.JsonDocument).Replace("\"", "\"\"")}\");\r\n"));

        var sb = new StringBuilder();
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
        // Also run static constructors for closed generics — open generic static fields
        // are per-closed-type, so PropertyKey registrations only fire on a closed type.
        if (closedGenerics.Count > 0)
        {
            foreach (var kvp in closedGenerics)
            {
                var csName = kvp.Value; // e.g. "ComboBox<global::ZurfurGui.Controls.IComboBoxItemTextData>"
                sb.AppendIndentedLine(2, $"global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(global::ZurfurGui.Controls.{csName}).TypeHandle);");
            }
        }
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Register non-generic controls");
        if (!string.IsNullOrWhiteSpace(registerControls))
            sb.Append(registerControls).Append("\r\n");
        sb.Append("\r\n");
        if (!string.IsNullOrWhiteSpace(registerClosedGenerics))
        {
            sb.AppendIndentedLine(2, "// Register closed generic controls (discovered from usage sites)");
            sb.Append(registerClosedGenerics).Append("\r\n\r\n");
        }
        if (!string.IsNullOrWhiteSpace(registerStyles))
            sb.Append(registerStyles);
        if (!string.IsNullOrWhiteSpace(registerThemes))
            sb.Append(registerThemes);
        sb.AppendIndentedLine(1, "}");
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// Recursively scan a JSON document for .controller values that are closed generics
    /// (e.g. ".controller": "ComboBox&lt;ComboBoxItemText&gt;" at a usage site).
    /// Skips open generic definitions where the type argument matches the source file's own TypeParam.
    /// Translates the control-name type argument to its data interface name using controlList
    /// (e.g. ComboBoxItemText -> IComboBoxItemTextData), producing the C# type name for registration.
    /// The dictionary key is the JSON name (Loader lookup key) and the value is the C# type name.
    /// </summary>
    static void CollectClosedGenerics(Dictionary<string, object?> json,
        ZuiFileInfo sourceFile, List<ZuiFileInfo> controlList,
        SortedDictionary<string, string> result)
    {
        foreach (var kvp in json)
        {
            if (kvp.Key == ".controller" && kvp.Value is string controllerValue)
            {
                var match = s_closedGenericRegex.Match(controllerValue);
                if (match.Success)
                {
                    var baseName = match.Groups[1].Value;   // e.g. "ComboBox"
                    var typeArg = match.Groups[2].Value;    // e.g. "ComboBoxItemText"
                    // Skip the open generic definition's own controller value
                    if (typeArg != sourceFile.TypeParam && !result.ContainsKey(controllerValue))
                    {
                        // Look up the namespace of the type arg control in controlList
                        var typeArgInfo = controlList.FirstOrDefault(c => c.ControllerName == typeArg);
                        var typeArgNs = typeArgInfo != null ? typeArgInfo.Namespace : "ZurfurGui.Controls";
                        // Fully qualify the data interface: ComboBoxItemText -> global::ZurfurGui.Controls.IComboBoxItemTextData
                        var csTypeArg = $"global::{typeArgNs}.I{typeArg}Data";
                        var csName = $"{baseName}<{csTypeArg}>";
                        result[controllerValue] = csName;
                    }
                }
            }

            if (kvp.Value is Dictionary<string, object?> nested)
                CollectClosedGenerics(nested, sourceFile, controlList, result);

            if (kvp.Value is List<object?> list)
                foreach (var item in list)
                    if (item is Dictionary<string, object?> listItem)
                        CollectClosedGenerics(listItem, sourceFile, controlList, result);
        }
    }
}
