using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitData
{
    internal static string GenerateDataImplementationSource(ZuiTypes.FileInfo data, List<ZuiTypes.DataBinding>? inheritedBindings)
    {
        // All bindings: inherited ones first, then the control's own bindings.
        var allBindings = inheritedBindings != null
            ? inheritedBindings.Concat(data.Bindings).ToList()
            : data.Bindings;

        if (allBindings.Count == 0)
            return "";

        // When .implements is set, inherited bindings are delegated to an instance of the base
        // data class rather than re-implemented here.  Own bindings are still fully generated.
        bool hasImplements = data.Implements != "" && inheritedBindings != null && inheritedBindings.Count > 0;
        var ownBindings = data.Bindings;

        // Verify bindings are valid
        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        foreach (var binding in data.Bindings)
        {
            if (binding.Bind == "")
            {
                throw new Exception($"The binding for '{binding.Name}' must not be empty. "
                    + "It can be 'styled', 'data', or a valid control name.");
            }

            // Reserved words: 'styled' creates a property+data item, 'data' stores only in the data object
            if (binding.Bind == "styled" || binding.Bind == "data")
                continue;

            var bindingPath = binding.Bind.Split('.');
            var bindingName = bindingPath[0];

            if (!ZuiEmit.IsNamedControl(bindingName, namedControls))
            {
                throw new Exception($"The binding for '{binding.Name}' does not match a valid control name. Binding: '{binding.Bind}'");
            }

        }

        var interfaceName = $"I{data.ControllerName}Data";
        var className = $"{data.ControllerName}Data";
        var implementsClassName = hasImplements ? $"{data.Implements}Data" : "";
        // Named __implement{Implements} rather than __base to avoid implying C# inheritance
        // and to leave room for multiple implements in the future.
        var implementsFieldName = hasImplements ? $"__implement{data.Implements}" : "";

        // Generic data classes mirror the controller's type parameter and constraint.
        var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
        var genericConstraint = data.TypeParam != ""
            ? $"\r\n    where {data.TypeParam} : I{data.TypeParamConstraint}Data"
            : "";

        var partialKeyword = data.UserSuppliedDataClass ? "partial " : "";

        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public sealed {partialKeyword}class {className}{genericSuffix}"
            + $" : {interfaceName}{genericSuffix}{genericConstraint}\r\n{{\r\n");

        if (hasImplements)
        {
            // Delegate inherited properties to a dedicated instance of the implemented data class.
            // This avoids duplicating backing fields and PropertyChangedEventArgs for properties
            // that are already fully implemented there.  PropertyChanged events are forwarded
            // onto this instance so subscribers see a unified event stream.
            // Named __implement{Implements} rather than __base to avoid implying C# inheritance
            // and to leave room for multiple implements in the future.
            sb.AppendIndentedLine(1, $"// Inherited properties are delegated to a dedicated instance of {implementsClassName}.");
            sb.AppendIndentedLine(1, $"readonly {implementsClassName} {implementsFieldName} = new();");
            sb.Append("\r\n");
        }

        // Generate static PropertyChangedEventArgs only for own bindings when delegating,
        // or for all bindings when not.
        var eventArgsBindings = hasImplements ? ownBindings : allBindings;
        foreach (var p in eventArgsBindings)
        {
            var csName = ZuiEmit.ToPascalCase(p.Name);
            sb.AppendIndentedLine(1, $"static readonly PropertyChangedEventArgs s_{p.Name}EventArgs = new(nameof({csName}));");
        }
        sb.Append("\r\n");

        // Generate backing fields only for own bindings when delegating.
        var backingFieldBindings = hasImplements ? ownBindings : allBindings;
        foreach (var p in backingFieldBindings)
            sb.AppendIndentedLine(1, $"{ZuiEmit.GetBindingDataType(p, namedControls)} __{p.Name};");
        sb.Append("\r\n");

        // Parameterless constructor
        sb.AppendIndentedLine(1, $"public {className}()");
        sb.AppendIndentedLine(1, "{");
        if (hasImplements)
        {
            // Forward PropertyChanged from the implement instance so subscribers on this object
            // receive notifications for inherited properties without any extra per-property code.
            sb.AppendIndentedLine(2, $"{implementsFieldName}.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);");
        }
        foreach (var binding in backingFieldBindings)
        {
            var backingFieldName = $"__{binding.Name}";
            if (ZuiEmit.IsNamedControl(binding.Bind, namedControls))
                sb.AppendIndentedLine(2, $"{backingFieldName} = new {binding.BaseType}Data();");
            else if (binding.IsCollection)
                sb.AppendIndentedLine(2, $"{backingFieldName} = new {ZuiEmit.GetBindingDataType(binding, namedControls)}();");
            else if (binding.IsNullable)
                sb.AppendIndentedLine(2, $"{backingFieldName} = null;");
            else
                sb.AppendIndentedLine(2, $"{backingFieldName} = new {binding.BaseType}();");
        }
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // Constructor that accepts each binding value (all bindings — inherited + own)
        var ctorParams = string.Join(", ", allBindings.Select(b => $"{ZuiEmit.GetBindingDataType(b, namedControls)} {b.Name}"));
        sb.AppendIndentedLine(1, $"public {className}({ctorParams})");
        sb.AppendIndentedLine(1, "{");
        if (hasImplements)
        {
            // Forward PropertyChanged from the implement instance.
            sb.AppendIndentedLine(2, $"{implementsFieldName}.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);");
            // Assign inherited params via the delegating properties (which route through __implement*).
            foreach (var binding in inheritedBindings!)
                sb.AppendIndentedLine(2, $"{ZuiEmit.ToPascalCase(binding.Name)} = {binding.Name};");
        }
        foreach (var binding in backingFieldBindings)
            sb.AppendIndentedLine(2, $"__{binding.Name} = {binding.Name};");
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // INotifyPropertyChanged
        sb.AppendIndentedLine(1, "public event PropertyChangedEventHandler? PropertyChanged;");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void OnPropertyChanged(PropertyChangedEventArgs args)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "PropertyChanged?.Invoke(this, args);");
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // Delegating properties for inherited bindings
        if (hasImplements)
        {
            foreach (var p in inheritedBindings!)
            {
                var propertyType = ZuiEmit.GetBindingDataType(p, namedControls);
                var csName = ZuiEmit.ToPascalCase(p.Name);
                sb.AppendIndentedLine(1, $"public {propertyType} {csName}");
                sb.AppendIndentedLine(1, "{");
                sb.AppendIndentedLine(2, $"get => {implementsFieldName}.{csName};");
                sb.AppendIndentedLine(2, $"set => {implementsFieldName}.{csName} = value;");
                sb.AppendIndentedLine(1, "}");
                sb.Append("\r\n");
            }
        }

        // Full properties for own bindings (or all bindings when not delegating)
        foreach (var p in backingFieldBindings)
        {
            var propertyType = ZuiEmit.GetBindingDataType(p, namedControls);
            var backingField = $"__{p.Name}";
            var eventArgsField = $"s_{p.Name}EventArgs";
            sb.AppendIndentedLine(1, $"public {propertyType} {ZuiEmit.ToPascalCase(p.Name)}");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"get => {backingField};");
            sb.AppendIndentedLine(2, "set");
            sb.AppendIndentedLine(2, "{");
            sb.AppendIndentedLine(3, $"if (!EqualityComparer<{propertyType}>.Default.Equals({backingField}, value))");
            sb.AppendIndentedLine(3, "{");
            sb.AppendIndentedLine(4, $"{backingField} = value;");
            sb.AppendIndentedLine(4, $"OnPropertyChanged({eventArgsField});");
            sb.AppendIndentedLine(3, "}");
            sb.AppendIndentedLine(2, "}");
            sb.AppendIndentedLine(1, "}");
            sb.Append("\r\n");
        }

        sb.Append("}");
        return sb.ToString();
    }
}
