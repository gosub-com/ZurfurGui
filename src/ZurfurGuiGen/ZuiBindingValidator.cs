using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;

/// <summary>
/// Validates forwarded ZUI data bindings against the nullability of their source properties.
///
/// How it works: A forwarded binding such as <c>bind: "data:_itemText.text"</c> copies the value of
/// the named control's data property (here <c>_itemText</c>, a TextView, property <c>Text</c>) into
/// the data property declared in the same .zui file.  If the source property is declared nullable
/// (e.g. <c>type: "?TextLines"</c>) but the destination is not (e.g. <c>type: "TextLines"</c>),
/// the generated code would assign a possibly null value to a non-nullable property, which the C#
/// compiler reports as CS8601.  This class finds that mismatch up front and reports ZUI006, which
/// names both ends of the binding.  The steps are:
///   1. Find the named controls in the .zui file (control name to controller type).
///   2. For each non-nullable, non-collection, forwarded binding with a two part path
///      ("control.property"), look up the control's controller type.
///   3. Find the one .zui file in this project whose controller name matches that type.
///   4. Find the property in that file's bindings and check whether it is nullable.
///
/// Limitations:
///   - Only .zui files in the same project (compilation) are searched.  If the source controller
///     comes from another library, no .zui data is available, so the binding is silently skipped.
///   - Only simple "control.property" paths are checked.  Other path shapes are skipped.
///   - Collection bindings and bindings that already allow null are not checked.
///   - If zero or several .zui files match the controller name, the binding is skipped.
///   - Only the .zui declarations are compared.  Hand written data classes are not inspected.
///   - The diagnostic location is the start of the file, not the line of the binding.
/// </summary>
internal static class ZuiBindingValidator
{
    /// <summary>
    /// Finds forwarded bindings from nullable source properties to non-nullable data properties.
    /// </summary>
    internal static IEnumerable<Diagnostic> GetNullableBindingDiagnostics(
        ZuiFileInfo data,
        ImmutableArray<ZuiFileInfo> allZuiData)
    {
        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        foreach (var binding in data.Bindings)
        {
            if (binding.BindType != BindType.Forwarded || binding.IsNullable || binding.IsCollection)
                continue;

            var pathParts = binding.Bind.Split('.');
            if (pathParts.Length != 2 || !namedControls.TryGetValue(pathParts[0], out var namedControl))
                continue;

            var controllerName = namedControl.Type;
            var genericStart = controllerName.IndexOf('<');
            if (genericStart >= 0)
                controllerName = controllerName.Substring(0, genericStart);

            var sourceControllers = allZuiData
                .Where(candidate => candidate.Diagnostic == null
                    && candidate.ControllerName == controllerName)
                .ToArray();
            if (sourceControllers.Length != 1)
                continue;

            var sourcePropertyName = ZuiEmit.ToPascalCase(pathParts[1]);
            var sourceBinding = sourceControllers[0].Bindings.FirstOrDefault(candidate =>
                candidate.PascalName == sourcePropertyName);
            if (sourceBinding == null || !sourceBinding.IsNullable)
                continue;

            var location = Location.Create(data.Path, new(), new());
            yield return ZuiDiagnostics.GetNullableBindingDiagnostic(
                location,
                data.FileName + ".zui",
                binding.Name,
                binding.BaseType,
                binding.Bind,
                controllerName,
                sourceBinding.PascalName,
                sourceBinding.NullableType);
        }
    }
}
