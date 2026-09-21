using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;

internal static class ZuiEmitData
{
    internal static string GenerateDataImplementationSource(ZuiFileInfo data)
    {
        var bindings = data.Bindings.Where(b => b.BindType != BindType.StyledOnly).ToList();
        if (bindings.Count == 0)
            return "";

        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        foreach (var binding in data.Bindings)
        {
            if (binding.BindType != BindType.Forwarded)
                continue;

            var bindingPath = binding.Bind.Split('.');
            if (!ZuiEmit.IsNamedControl(bindingPath[0], namedControls))
                throw new Exception($"The binding for '{binding.Name}' does not match a valid control name. Binding: '{binding.Bind}'");
        }

        var className = $"{data.ControllerName}Data";
        var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
        var genericConstraint = data.TypeParam != ""
            ? $"\r\n    where {data.TypeParam} : {ZuiEmit.GetConstraintType(data.TypeParamConstraint)}"
            : "";
        var partialKeyword = data.UserSuppliedDataClass ? "partial " : "";

        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");
        sb.Append($"public sealed {partialKeyword}class {className}{genericSuffix} : INotifyPropertyChanged{genericConstraint}\r\n{{\r\n");

        foreach (var binding in bindings)
            sb.AppendIndentedLine(1, $"static readonly PropertyChangedEventArgs s_{binding.Name}EventArgs = new(nameof({binding.PascalName}));");
        sb.Append("\r\n");

        foreach (var binding in bindings)
            sb.AppendIndentedLine(1, $"{ZuiEmit.GetBindingDataType(binding, namedControls)} __{binding.Name};");
        sb.Append("\r\n");

        sb.AppendIndentedLine(1, $"public {className}()");
        sb.AppendIndentedLine(1, "{");
        foreach (var binding in bindings)
        {
            var fieldName = $"__{binding.Name}";
            if (!string.IsNullOrWhiteSpace(binding.Default))
                sb.AppendIndentedLine(2, $"{fieldName} = {ZuiEmit.NormalizeDefaultValue(binding.Default)};");
            else if (ZuiEmit.IsNamedControl(binding.Bind, namedControls))
                sb.AppendIndentedLine(2, $"{fieldName} = new {binding.BaseType}Data();");
            else if (binding.IsCollection)
                sb.AppendIndentedLine(2, $"{fieldName} = new {ZuiEmit.GetBindingDataType(binding, namedControls)}();");
            else if (binding.IsNullable)
                sb.AppendIndentedLine(2, $"{fieldName} = null;");
            else
                sb.AppendIndentedLine(2, $"{fieldName} = new {binding.BaseType}();");
        }
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        var ctorParams = string.Join(", ", bindings.Select(b => $"{ZuiEmit.GetBindingDataType(b, namedControls)} {b.Name}"));
        sb.AppendIndentedLine(1, $"public {className}({ctorParams})");
        sb.AppendIndentedLine(1, "{");
        foreach (var binding in bindings)
            sb.AppendIndentedLine(2, $"__{binding.Name} = {binding.Name};");
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        sb.AppendIndentedLine(1, "public event PropertyChangedEventHandler? PropertyChanged;");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void OnPropertyChanged(PropertyChangedEventArgs args)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "PropertyChanged?.Invoke(this, args);");
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        foreach (var binding in bindings)
        {
            var propertyType = ZuiEmit.GetBindingDataType(binding, namedControls);
            var backingField = $"__{binding.Name}";
            sb.AppendIndentedLine(1, $"public {propertyType} {binding.PascalName}");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"get => {backingField};");
            sb.AppendIndentedLine(2, "set");
            sb.AppendIndentedLine(2, "{");
            sb.AppendIndentedLine(3, $"if (!EqualityComparer<{propertyType}>.Default.Equals({backingField}, value))");
            sb.AppendIndentedLine(3, "{");
            sb.AppendIndentedLine(4, $"{backingField} = value;");
            sb.AppendIndentedLine(4, $"OnPropertyChanged(s_{binding.Name}EventArgs);");
            sb.AppendIndentedLine(3, "}");
            sb.AppendIndentedLine(2, "}");
            sb.AppendIndentedLine(1, "}");
            sb.Append("\r\n");
        }

        sb.Append("}");
        return sb.ToString();
    }
}
