using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitContract
{
    internal static string GenerateContractInterfaceSource(ZuiTypes.FileInfo data,
        IEnumerable<string>? inheritedBindingNames = null)
    {
        if (data.Bindings.Count == 0)
            return "";

        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);

        var interfaceName = $"I{data.ControllerName}Data";

        // When .implements is set, this data interface extends the constraint's data interface
        // (e.g. IComboBoxItemTextData : IComboBoxItemData) so that it satisfies
        // ObservableCollection<IComboBoxItemData> without an explicit cast.
        var dataBaseInterface = data.Implements != ""
            ? $"I{data.Implements}Data"
            : "INotifyPropertyChanged";

        // Generic controls emit a generic data interface (e.g. IComboBoxData<Item> where Item : IComboBoxItemData)
        var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
        var genericConstraint = data.TypeParam != ""
            ? $"\r\n    where {data.TypeParam} : I{data.TypeParamConstraint}Data"
            : "";

        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Generate data contract
        sb.Append($"/// <summary>\r\n");
        sb.Append($"/// Data contract interface for <see cref=\"{data.ControllerName}\"/>.");
        sb.Append($" Generated from <c>{Path.GetFileName(data.Path)}</c>.\r\n");
        sb.Append($"/// Bind view data properties through this interface;");
        sb.Append($" do not reference the concrete data class directly.\r\n");
        sb.Append($"/// </summary>\r\n");
        sb.Append($"public interface {interfaceName}{genericSuffix} : {dataBaseInterface}{genericConstraint}\r\n{{\r\n");
        var inherited = inheritedBindingNames != null ? new HashSet<string>(inheritedBindingNames) : null;
        foreach (var binding in data.Bindings)
        {
            // Skip properties already declared in the base constraint interface
            if (inherited != null && inherited.Contains(binding.Name))
                continue;
            var csName = ZuiEmit.ToPascalCase(binding.Name);
            ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
            sb.AppendIndentedLine(1, $"{ZuiEmit.GetBindingDataType(binding, namedControls)} {csName} {{ get; set; }}");
        }
        sb.Append("}");

        // Generate constraint contract
        // Don't allow generic container controls as a constraint
        if (data.TypeParam == "")
        {
            sb.Append($"\r\n\r\n/// <summary>\r\n");
            sb.Append($"/// Controller interface for <see cref=\"{data.ControllerName}\"/>.");
            sb.Append($" Generated from <c>{Path.GetFileName(data.Path)}</c>.\r\n");
            sb.Append($"/// Used as a type constraint (e.g. <c>where TItem : I{data.ControllerName}</c>) so that\r\n");
            sb.Append($"/// container controls such as <c>ComboBox</c> can work with any conforming item control\r\n");
            sb.Append($"/// without depending on a concrete type.\r\n");
            sb.Append($"/// </summary>\r\n");
            sb.Append($"public interface I{data.ControllerName}\r\n{{\r\n");
            sb.AppendIndentedLine(1, $"I{data.ControllerName}Data DataContext {{ get; set; }}");
            sb.Append("}");
        }


        return sb.ToString();
    }
}
