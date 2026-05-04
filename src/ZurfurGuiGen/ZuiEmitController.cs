using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitController
{
    internal static string GenerateControllerClassSource(ZuiTypes.FileInfo data)
    {
        // Serialize JSON and escape double quotes for verbatim string
        var zuiJsonContent = Json.Serialize(data.JsonDocument).Replace("\"", "\"\"");

        // Add file header, usings, and namespace
        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Add class header
        var partialKeyword = data.UserSuppliedControllerClass ? "partial " : "";
        sb.Append($"public sealed {partialKeyword}class {data.FileName} : global::ZurfurGui.Base.Controllable\r\n{{\r\n");

        // Add class variables
        sb.AppendIndentedLine(1, "public global::ZurfurGui.Base.View View { get; private set; } = null!; // Set by InitializeControl");
        sb.AppendIndentedLine(1, $"public string TypeName => \"{data.ControllerName}\";");
        sb.AppendIndentedLine(1, $"public string TypeNamespace => \"{data.Namespace}\";");
        sb.AppendIndentedLine(1, $"public TextLines TypeUses => new TextLines([{string.Join(",",
            data.Use.Select(s => "\"" + s + "\""))}]);");

        // Generated PropertyKey fields for ".data" entries that bind to "new"
        var newBindings = data.Bindings.Where(b => b.Bind == "new").ToList();
        if (newBindings.Count > 0)
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(1, "// Property Keys");
            foreach (var binding in newBindings)
            {
                var propertyName = ZuiEmit.ToPascalCase(binding.Name);
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {propertyName}"
                        + $" = new(\"{data.ControllerName}.{binding.Name}\", typeof({data.ControllerName}), new());");
            }
        }

        // Generate data property info dictionary
        GenerateDataPropertyInfoDictionary(data, sb);

        // Data bindings - generate full property with event hookup if there are "new" bindings       
        GenerateDataContextProperty(data, sb);
        sb.Append("\r\n");

        // Create named control variables
        var namedControlsDict = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        var controlNames = namedControlsDict.Keys.OrderBy(n => n);
        sb.AppendIndentedLine(1, "// Named controls (public unless name starts with '_')");
        foreach (var name in controlNames)
        {
            var qualifier = name.StartsWith("_") ? "private" : "public ";
            sb.AppendIndentedLine(1, $"{qualifier} {namedControlsDict[name]} {name} = null!; // Set by InitializeControl");
        }

        // Add constructor if no .cs file is supplied
        var constructor = "";
        if (!data.UserSuppliedControllerClass)
        {
            // .cs class is not supplied (create a constructor)
            constructor = $"    // No .cs file detected, so generate constructor\r\n"
                + $"    public {data.FileName}()\r\n    {{\r\n        InitializeControl();\r\n    }}\r\n";
            sb.Append("\r\n").Append(constructor);
        }

        // Add InitializeControl header
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void InitializeControl()");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "View = new(this);");
        sb.AppendIndentedLine(2, "global::ZurfurGui.Loader.Load(this, _zuiJsonContent);");
        sb.Append("\r\n");

        // Add InitializeControl code to initialize named controls
        sb.AppendIndentedLine(2, "// Initialize named controls");
        foreach (var name in controlNames)
            sb.AppendIndentedLine(2, $"{name} = ({namedControlsDict[name]})View.FindByName(\"{name}\").Controller;");

        // Initialize DataContext after controls are loaded
        if (data.Bindings.Count != 0)
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(2, "// Initialize DataContext");
            sb.AppendIndentedLine(2, "DataContext = CreateDefaultDataContext();");
        }

        // Always apply data properties recursively (for self and all children)
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Apply data properties from JSON (recursively)");
        sb.AppendIndentedLine(2, "global::ZurfurGui.Loader.ApplyDataProperties(this);");

        sb.AppendIndentedLine(1, "}");

        // Generate CreateDefaultDataContext factory method
        if (data.Bindings.Count != 0)
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(1, $"I{data.ControllerName}Data CreateDefaultDataContext()");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"return new {data.ControllerName}Data(");

            var args = new List<string>();
            foreach (var binding in data.Bindings)
            {
                if (ZuiEmit.IsNamedControl(binding.Bind, namedControlsDict))
                {
                    // If this binds directly to a named control, use the data context
                    args.Add($"{binding.Name}: {binding.Bind}.DataContext");
                }
                else
                {
                    // For all other types, initialize with new instance
                    if (binding.IsNullable)
                        args.Add($"{binding.Name}: null");
                    else
                        args.Add($"{binding.Name}: new {binding.BaseType}()");
                }
            }

            for (int i = 0; i < args.Count; i++)
            {
                var suffix = i < args.Count - 1 ? "," : "";
                sb.AppendIndentedLine(3, args[i] + suffix);
            }

            sb.AppendIndentedLine(2, ");");
            sb.AppendIndentedLine(1, "}");

            // Generate event handler for DataContext property changes (only if there are "new" bindings)
            GenerateOnDataContextPropertyChanged(data, sb);
            GenerateSyncAllPropertiesToView(data, sb);
            GenerateSetDataProperty(data, sb, namedControlsDict);

        }

        // Access to JSON content
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");
        sb.Append("}");
        return sb.ToString();
    }

    private static void GenerateDataContextProperty(ZuiTypes.FileInfo data, StringBuilder sb)
    {
        if (data.Bindings.Count == 0)
            return;

        // Generate backing field and full property with event hookup
        sb.AppendIndentedLine(1, $"I{data.ControllerName}Data _dataContext = null!; // Set by InitializeControl");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, $"public I{data.ControllerName}Data DataContext");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "get => _dataContext;");
        sb.AppendIndentedLine(2, "set");
        sb.AppendIndentedLine(2, "{");
        sb.AppendIndentedLine(3, "if (_dataContext == value) return;");
        sb.AppendIndentedLine(3, "if (_dataContext != null)");
        sb.AppendIndentedLine(4, "_dataContext.PropertyChanged -= OnDataContextPropertyChanged;");
        sb.AppendIndentedLine(3, "_dataContext = value;");
        sb.AppendIndentedLine(3, "if (_dataContext != null)");
        sb.AppendIndentedLine(3, "{");
        sb.AppendIndentedLine(4, "_dataContext.PropertyChanged += OnDataContextPropertyChanged;");
        sb.AppendIndentedLine(4, "SyncAllPropertiesToView();");
        sb.AppendIndentedLine(3, "}");
        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateOnDataContextPropertyChanged(ZuiTypes.FileInfo data, StringBuilder sb)
    {
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void OnDataContextPropertyChanged(object ?sender, PropertyChangedEventArgs e)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "switch (e.PropertyName)");
        sb.AppendIndentedLine(2, "{");

        // Generate a case for each binding 
        foreach (var binding in data.Bindings)
        {
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);
            sb.AppendIndentedLine(3, $"case \"{pascalName}\":");

            // Handle "new" bindings
            if (binding.Bind == "new")
            {
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(4, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                    sb.AppendIndentedLine(5, $"View.SetProperty({pascalName}, nonNull{pascalName});");
                    sb.AppendIndentedLine(4, "else");
                    sb.AppendIndentedLine(5, $"View.RemoveProperty({pascalName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(4, $"View.SetProperty({pascalName}, DataContext.{pascalName});");
                }
            }
            else
            {
                // Handle forwarding bindings
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(4, "// TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(4, $"{targetPath} = DataContext.{pascalName};");
                }
            }

            sb.AppendIndentedLine(4, "break;");
        }

        // Handle null or empty PropertyName (means all properties changed)
        sb.AppendIndentedLine(3, "case null:");
        sb.AppendIndentedLine(3, "case \"\":");
        sb.AppendIndentedLine(4, "SyncAllPropertiesToView();");
        sb.AppendIndentedLine(4, "break;");

        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSyncAllPropertiesToView(ZuiTypes.FileInfo data, StringBuilder sb)
    {
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void SyncAllPropertiesToView()");
        sb.AppendIndentedLine(1, "{");

        foreach (var binding in data.Bindings)
        {
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);

            // Handle "new" bindings
            if (binding.Bind == "new")
            {
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(2, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                    sb.AppendIndentedLine(3, $"View.SetProperty({pascalName}, nonNull{pascalName});");
                    sb.AppendIndentedLine(2, "else");
                    sb.AppendIndentedLine(3, $"View.RemoveProperty({pascalName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(2, $"View.SetProperty({pascalName}, DataContext.{pascalName});");
                }
            }
            else
            {
                // Handle forwarding bindings
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(2, "// TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(2, $"{targetPath} = DataContext.{pascalName};");
                }
            }
        }

        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSetDataProperty(ZuiTypes.FileInfo data, StringBuilder sb, Dictionary<string, string> namedControls)
    {
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public bool SetDataProperty(string name, object? value)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "switch (name)");
        sb.AppendIndentedLine(2, "{");

        foreach (var binding in data.Bindings)
        {
            var jsonName = binding.Name; // Keep for error messages
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);
            var dataType = ZuiEmit.GetBindingDataType(binding, namedControls);
            var baseType = binding.BaseType;

            // If binding to a named control, use the interface type for pattern matching
            var matchType = ZuiEmit.IsNamedControl(binding.Bind, namedControls) 
                ? $"I{baseType}Data" 
                : baseType;

            sb.AppendIndentedLine(3, $"case \"{pascalName}\":");

            // Use pattern matching to handle nullable/non-nullable scenarios
            if (binding.IsNullable)
            {
                // Target is nullable - accept null or the base type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{pascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = typedValue{pascalName};");
                sb.AppendIndentedLine(4, $"else if (value is null)");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = null;");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
            }
            else
            {
                // Target is non-nullable - must be the correct type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{pascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = typedValue{pascalName};");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
            }

            sb.AppendIndentedLine(4, "return true;");
        }

        sb.AppendIndentedLine(3, "default:");
        sb.AppendIndentedLine(4, "return false;");
        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    /// <summary>
    /// Transform a binding path like "_checkText.text" to "_checkText.DataContext.Text".
    /// Inserts "DataContext" after the first segment and PascalCases remaining segments.
    /// </summary>
    static string TransformBindingPath(string bindPath)
    {
        var parts = bindPath.Split('.');
        if (parts.Length == 1)
        {
            // Edge case: no '.' in the path
            return bindPath + ".DataContext";
        }

        var controlRef = parts[0];
        var remainingParts = parts.Skip(1).Select(ZuiEmit.ToPascalCase);
        return controlRef + ".DataContext." + string.Join(".", remainingParts);
    }

    private static void GenerateDataPropertyInfoDictionary(ZuiTypes.FileInfo data, StringBuilder sb)
    {
        sb.Append("\r\n");

        if (data.Bindings.Count == 0)
        {
            // Generate empty dictionary for controls without data bindings
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, DataPropertyInfo> s_dataPropertyInfo = new();");
        }
        else
        {
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, DataPropertyInfo> s_dataPropertyInfo = new()");
            sb.AppendIndentedLine(1, "{");

            for (int i = 0; i < data.Bindings.Count; i++)
            {
                var binding = data.Bindings[i];
                var pascalName = ZuiEmit.ToPascalCase(binding.Name);
                var comma = i < data.Bindings.Count - 1 ? "," : "";
                var nullableStr = binding.IsNullable ? "true" : "false";
                sb.AppendIndentedLine(2, $"[\"{pascalName}\"] = new(\"{pascalName}\", typeof({binding.BaseType}), {nullableStr}){comma}");
            }

            sb.AppendIndentedLine(1, "};");
        }

        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public IReadOnlyDictionary<string, DataPropertyInfo> DataPropertyInfo => s_dataPropertyInfo;");
    }
}
