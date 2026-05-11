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

        // Verify bindings are valid
        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        foreach (var binding in data.Bindings)
        {
            if (binding.Bind == "")
            {
                throw new Exception($"The binding for '{binding.Name}' must not be empty. "
                    + "It can be 'new' or a valid control name.");
            }

            // Reserved word to create a new data instance instead of binding to a control
            if (binding.Bind == "new")
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
        // No need to explicitly implement I{Implements}Data here — IControllerNameData
        // already extends it via the generated data contract interface chain.

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

        // Generate static PropertyChangedEventArgs for each property
        foreach (var p in allBindings)
        {
            var csName = ZuiEmit.ToPascalCase(p.Name);
            sb.AppendIndentedLine(1, $"static readonly PropertyChangedEventArgs s_{p.Name}EventArgs = new(nameof({csName}));");
        }
        sb.Append("\r\n");

        // Generate backing fields
        foreach (var p in allBindings)
            sb.AppendIndentedLine(1, $"{ZuiEmit.GetBindingDataType(p, namedControls)} __{p.Name};");
        sb.Append("\r\n");

        // Parameterless constructor initializing default values
        // (mirrors the controller's CreateDefaultDataContext initialization)
        sb.AppendIndentedLine(1, $"public {className}()");
        sb.AppendIndentedLine(1, "{");
        foreach (var binding in allBindings)
        {
            // Generate backing field for the data
            var backingFieldName = $"__{binding.Name}";
            if (ZuiEmit.IsNamedControl(binding.Bind, namedControls))
            {
                // Target named control (e.g. "bind": "_card1")
                sb.AppendIndentedLine(2, $"{backingFieldName} = new {binding.BaseType}Data();");
            }
            else if (binding.IsCollection)
            {
                // Collection type: initialize with empty ObservableCollection
                sb.AppendIndentedLine(2, $"{backingFieldName} = new {ZuiEmit.GetBindingDataType(binding, namedControls)}();");
            }
            else
            {
                // Target non-control type
                if (binding.IsNullable)
                    sb.AppendIndentedLine(2, $"{backingFieldName} = null;");
                else
                    sb.AppendIndentedLine(2, $"{backingFieldName} = new {binding.BaseType}();");
            }
        }
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // Constructor that accepts each binding value
        var ctorParams = string.Join(", ", allBindings.Select(b => $"{ZuiEmit.GetBindingDataType(b, namedControls)} {b.Name}"));
        sb.AppendIndentedLine(1, $"public {className}({ctorParams})");
        sb.AppendIndentedLine(1, "{");
        foreach (var binding in allBindings)
        {
            var paramName = binding.Name;
            var backingFieldName = $"__{paramName}";
            sb.AppendIndentedLine(2, $"{backingFieldName} = {paramName};");
        }
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // Generate INotifyPropertyChanged implementation
        sb.AppendIndentedLine(1, "public event PropertyChangedEventHandler? PropertyChanged;");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void OnPropertyChanged(PropertyChangedEventArgs args)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "PropertyChanged?.Invoke(this, args);");
        sb.AppendIndentedLine(1, "}");
        sb.Append("\r\n");

        // Generate properties with INotifyPropertyChanged implementation
        foreach (var p in allBindings)
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
