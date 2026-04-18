using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmit
{
    static void AppendFileHeader(StringBuilder sb, string sourceFileName)
    {
        sb.Append($"// This file is generated from '{sourceFileName}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n");
    }

    static void AppendIndentedLine(StringBuilder sb, int indentLevel, string line)
    {
        for (var i = 0; i < indentLevel; i++)
            sb.Append("    ");
        sb.Append(line);
        sb.Append("\r\n");
    }

    static string GenerateUsingCode(ZuiTypes.FileInfo data)
    {
        var sb = new StringBuilder();
        sb.Append("using System.ComponentModel;\r\n");
        sb.Append("using ZurfurGui.Base;\r\n");
        sb.Append("using ZurfurGui.Property;\r\n");
        sb.Append("using ZurfurGui.Controls;\r\n\r\n");

        if (data.Use != null && data.Use.Count != 0)
            foreach (var u in data.Use)
                sb.Append($"using {u};\r\n");

        sb.Append("\r\n");

        return sb.ToString();
    }

    internal static string GenerateContractInterfaceSource(ZuiTypes.FileInfo data)
    {
        if (data.Bindings.Count == 0)
            return "";

        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);

        var interfaceName = $"I{data.ControllerName}Data";

        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public interface {interfaceName} : INotifyPropertyChanged\r\n{{\r\n");
        foreach (var binding in data.Bindings)
        {
            var csName = ToPascalCase(binding.Name);
            AppendIndentedLine(sb, 1, $"{GetBindingDataType(binding, namedControls)} {csName} {{ get; set; }}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    internal static string GenerateDataImplementationSource(ZuiTypes.FileInfo data)
    {
        if (data.Bindings.Count == 0)
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

            if (!IsNamedControl(bindingName, namedControls))
            {
                throw new Exception($"The binding for '{binding.Name}' does not match a valid control name. Binding: '{binding.Bind}'");
            }

        }

        var interfaceName = $"I{data.ControllerName}Data";
        var className = $"{data.ControllerName}Data";

        var partialKeyword = data.UserSuppliedDataClass ? "partial " : "";

        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public sealed {partialKeyword}class {className} : {interfaceName}\r\n{{\r\n");

        // Generate static PropertyChangedEventArgs for each property
        foreach (var p in data.Bindings)
        {
            var csName = ToPascalCase(p.Name);
            AppendIndentedLine(sb, 1, $"static readonly PropertyChangedEventArgs s_{p.Name}EventArgs = new(nameof({csName}));");
        }
        sb.Append("\r\n");

        // Generate backing fields
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"{GetBindingDataType(p, namedControls)} __{p.Name};");
        sb.Append("\r\n");

        // Parameterless constructor initializing default values
        // (mirrors the controller's CreateDefaultDataContext initialization)
        AppendIndentedLine(sb, 1, $"public {className}()");
        AppendIndentedLine(sb, 1, "{");
        foreach (var binding in data.Bindings)
        {
            // Generate backing field for the data
            var backingFieldName = $"__{binding.Name}";
            if (IsNamedControl(binding.Bind, namedControls))
            {
                // Target named control (e.g. "bind": "_card1")
                AppendIndentedLine(sb, 2, $"{backingFieldName} = new {binding.BaseType}Data();");
            }
            else
            {
                // Target non-control type
                if (binding.IsNullable)
                    AppendIndentedLine(sb, 2, $"{backingFieldName} = null;");
                else
                    AppendIndentedLine(sb, 2, $"{backingFieldName} = new {binding.BaseType}();");
            }
        }
        AppendIndentedLine(sb, 1, "}");
        sb.Append("\r\n");

        // Constructor that accepts each binding value
        var ctorParams = string.Join(", ", data.Bindings.Select(b => $"{GetBindingDataType(b, namedControls)} {b.Name}"));
        AppendIndentedLine(sb, 1, $"public {className}({ctorParams})");
        AppendIndentedLine(sb, 1, "{");
        foreach (var binding in data.Bindings)
        {
            var paramName = binding.Name;
            var backingFieldName = $"__{paramName}";
            AppendIndentedLine(sb, 2, $"{backingFieldName} = {paramName};");
        }
        AppendIndentedLine(sb, 1, "}");
        sb.Append("\r\n");

        // Generate INotifyPropertyChanged implementation
        AppendIndentedLine(sb, 1, "public event PropertyChangedEventHandler PropertyChanged;");
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, "void OnPropertyChanged(PropertyChangedEventArgs args)");
        AppendIndentedLine(sb, 1, "{");
        AppendIndentedLine(sb, 2, "PropertyChanged?.Invoke(this, args);");
        AppendIndentedLine(sb, 1, "}");
        sb.Append("\r\n");

        // Generate properties with INotifyPropertyChanged implementation
        foreach (var p in data.Bindings)
        {
            var propertyType = GetBindingDataType(p, namedControls);
            var backingField = $"__{p.Name}";
            var eventArgsField = $"s_{p.Name}EventArgs";
            AppendIndentedLine(sb, 1, $"public {propertyType} {ToPascalCase(p.Name)}");
            AppendIndentedLine(sb, 1, "{");
            AppendIndentedLine(sb, 2, $"get => {backingField};");
            AppendIndentedLine(sb, 2, "set");
            AppendIndentedLine(sb, 2, "{");
            AppendIndentedLine(sb, 3, $"if (!EqualityComparer<{propertyType}>.Default.Equals({backingField}, value))");
            AppendIndentedLine(sb, 3, "{");
            AppendIndentedLine(sb, 4, $"{backingField} = value;");
            AppendIndentedLine(sb, 4, $"OnPropertyChanged({eventArgsField});");
            AppendIndentedLine(sb, 3, "}");
            AppendIndentedLine(sb, 2, "}");
            AppendIndentedLine(sb, 1, "}");
            sb.Append("\r\n");
        }


        sb.Append("}");
        return sb.ToString();
    }

    static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]))
            return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Use either the type name, or I{type}Data if it is a control
    /// </summary>
    static string GetBindingDataType(ZuiTypes.DataBinding binding, Dictionary<string, string> namedControls)
    {
        // If the binding targets a named control itself (e.g. "bind": "_card1"), the data-binding
        // type should be that control's data contract (I<ControlName>Data).
        if (IsNamedControl(binding.Bind, namedControls))
        {
            var controlTypeName = namedControls[binding.Bind];
            return $"I{controlTypeName}Data";
        }

        return binding.NullableType;
    }

    static bool IsNamedControl(string bind, Dictionary<string, string> namedControls)
        => namedControls.ContainsKey(bind);


    internal static string GenerateControllerClassSource(ZuiTypes.FileInfo data)
    {
        // Serialize JSON and escape double quotes for verbatim string
        var zuiJsonContent = Json.Serialize(data.JsonDocument).Replace("\"", "\"\"");

        // Add file header, usings, and namespace
        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Add class header
        var partialKeyword = data.UserSuppliedControllerClass ? "partial " : "";
        sb.Append($"public sealed {partialKeyword}class {data.FileName} : global::ZurfurGui.Base.Controllable\r\n{{\r\n");

        // Add class variables
        AppendIndentedLine(sb, 1, "public global::ZurfurGui.Base.View View { get; private set; }");
        AppendIndentedLine(sb, 1, $"public string TypeName => \"{data.ControllerName}\";");
        AppendIndentedLine(sb, 1, $"public string TypeNamespace => \"{data.Namespace}\";");
        AppendIndentedLine(sb, 1, $"public TextLines TypeUses => new TextLines([{string.Join(",",
            data.Use.Select(s => "\"" + s + "\""))}]);");

        // Generated PropertyKey fields for ".data" entries that bind to "new"
        foreach (var binding in data.Bindings)
        {
            if (binding.Bind == "new")
            {
                var propertyName = ToPascalCase(binding.Name);
                AppendIndentedLine(sb, 1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {propertyName}"
                        + $" = new(\"{binding.Name}\", typeof({data.ControllerName}), new(), ViewFlags.ReMeasure);");
            }
        }

        // Data bindings - generate full property with event hookup if there are "new" bindings
        var newBindings = data.Bindings.Where(b => b.Bind == "new").ToList();

        GenerateDataContextProperty(data, sb, newBindings);
        sb.Append("\r\n");

        // Create named control variables
        var namedControlsDict = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        var controlNames = namedControlsDict.Keys.OrderBy(n => n);
        AppendIndentedLine(sb, 1, "// Named controls (public unless name starts with '_')");
        foreach (var name in controlNames)
        {
            var qualifier = name.StartsWith("_") ? "private" : "public ";
            AppendIndentedLine(sb, 1, $"{qualifier} {namedControlsDict[name]} {name};");
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
        AppendIndentedLine(sb, 1, "void InitializeControl()");
        AppendIndentedLine(sb, 1, "{");
        AppendIndentedLine(sb, 2, "View = new(this);");
        AppendIndentedLine(sb, 2, "global::ZurfurGui.Loader.Load(this, _zuiJsonContent);");
        sb.Append("\r\n");

        // Add InitializeControl code to initialize named controls
        AppendIndentedLine(sb, 2, "// Initialize named controls");
        foreach (var name in controlNames)
            AppendIndentedLine(sb, 2, $"{name} = ({namedControlsDict[name]})View.FindByName(\"{name}\").Controller;");

        // Initialize DataContext after controls are loaded
        if (data.Bindings.Count != 0)
        {
            sb.Append("\r\n");
            AppendIndentedLine(sb, 2, "// Initialize DataContext");
            AppendIndentedLine(sb, 2, "DataContext = CreateDefaultDataContext();");
        }

        AppendIndentedLine(sb, 1, "}");

        // Generate CreateDefaultDataContext factory method
        if (data.Bindings.Count != 0)
        {
            sb.Append("\r\n");
            AppendIndentedLine(sb, 1, $"I{data.ControllerName}Data CreateDefaultDataContext()");
            AppendIndentedLine(sb, 1, "{");
            AppendIndentedLine(sb, 2, $"return new {data.ControllerName}Data(");

            var args = new List<string>();
            foreach (var binding in data.Bindings)
            {
                if (IsNamedControl(binding.Bind, namedControlsDict))
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
                AppendIndentedLine(sb, 3, args[i] + suffix);
            }

            AppendIndentedLine(sb, 2, ");");
            AppendIndentedLine(sb, 1, "}");

            // Generate event handler for DataContext property changes (only if there are "new" bindings)
            GenerateOnDataContextPropertyChanged(sb, newBindings);
        }

        // Access to JSON content
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");
        sb.Append("}");
        return sb.ToString();
    }

    private static void GenerateDataContextProperty(ZuiTypes.FileInfo data, StringBuilder sb, List<ZuiTypes.DataBinding> newBindings)
    {
        if (data.Bindings.Count == 0)
            return;

        if (newBindings.Count > 0)
        {
            // Generate backing field and full property with event hookup
            AppendIndentedLine(sb, 1, $"I{data.ControllerName}Data _dataContext;");
            sb.Append("\r\n");
            AppendIndentedLine(sb, 1, $"public I{data.ControllerName}Data DataContext");
            AppendIndentedLine(sb, 1, "{");
            AppendIndentedLine(sb, 2, "get => _dataContext;");
            AppendIndentedLine(sb, 2, "set");
            AppendIndentedLine(sb, 2, "{");
            AppendIndentedLine(sb, 3, "if (_dataContext == value) return;");
            AppendIndentedLine(sb, 3, "if (_dataContext != null)");
            AppendIndentedLine(sb, 4, "_dataContext.PropertyChanged -= OnDataContextPropertyChanged;");
            AppendIndentedLine(sb, 3, "_dataContext = value;");
            AppendIndentedLine(sb, 3, "if (_dataContext != null)");
            AppendIndentedLine(sb, 3, "{");
            AppendIndentedLine(sb, 4, "_dataContext.PropertyChanged += OnDataContextPropertyChanged;");
            AppendIndentedLine(sb, 4, "SyncAllPropertiesToView();");
            AppendIndentedLine(sb, 3, "}");
            AppendIndentedLine(sb, 2, "}");
            AppendIndentedLine(sb, 1, "}");
        }
        else
        {
            // No "new" bindings, use simple auto-property
            AppendIndentedLine(sb, 1, $"public I{data.ControllerName}Data DataContext {{ get; set; }}");
        }
    }

    private static void GenerateOnDataContextPropertyChanged(StringBuilder sb, List<ZuiTypes.DataBinding> newBindings)
    {
        if (newBindings.Count == 0)
            return;

        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, "void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)");
        AppendIndentedLine(sb, 1, "{");
        AppendIndentedLine(sb, 2, "switch (e.PropertyName)");
        AppendIndentedLine(sb, 2, "{");

        // Generate a case for each "new" binding
        foreach (var binding in newBindings)
        {
            var pascalName = ToPascalCase(binding.Name);
            AppendIndentedLine(sb, 3, $"case \"{pascalName}\":");

            if (binding.IsNullable)
            {
                // Generate code for nullable types: SetProperty or RemoveProperty
                AppendIndentedLine(sb, 4, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                AppendIndentedLine(sb, 5, $"View.SetProperty({pascalName}, nonNull{pascalName});");
                AppendIndentedLine(sb, 4, "else");
                AppendIndentedLine(sb, 5, $"View.RemoveProperty({pascalName});");
            }
            else
            {
                // Generate code for non-nullable types: SetProperty
                AppendIndentedLine(sb, 4, $"View.SetProperty({pascalName}, DataContext.{pascalName});");
            }

            AppendIndentedLine(sb, 4, "break;");
        }

        // Handle null or empty PropertyName (means all properties changed)
        AppendIndentedLine(sb, 3, "case null:");
        AppendIndentedLine(sb, 3, "case \"\":");
        AppendIndentedLine(sb, 4, "SyncAllPropertiesToView();");
        AppendIndentedLine(sb, 4, "break;");

        AppendIndentedLine(sb, 2, "}");
        AppendIndentedLine(sb, 1, "}");

        // Generate helper method to sync all properties
        GenerateSyncAllPropertiesToView(sb, newBindings);
    }

    private static void GenerateSyncAllPropertiesToView(StringBuilder sb, List<ZuiTypes.DataBinding> newBindings)
    {
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, "void SyncAllPropertiesToView()");
        AppendIndentedLine(sb, 1, "{");

        foreach (var binding in newBindings)
        {
            var pascalName = ToPascalCase(binding.Name);

            if (binding.IsNullable)
            {
                // Generate code for nullable types: SetProperty or RemoveProperty
                AppendIndentedLine(sb, 2, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                AppendIndentedLine(sb, 3, $"View.SetProperty({pascalName}, nonNull{pascalName});");
                AppendIndentedLine(sb, 2, "else");
                AppendIndentedLine(sb, 3, $"View.RemoveProperty({pascalName});");
            }
            else
            {
                // Generate code for non-nullable types: SetProperty
                AppendIndentedLine(sb, 2, $"View.SetProperty({pascalName}, DataContext.{pascalName});");
            }
        }

        AppendIndentedLine(sb, 1, "}");
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
        AppendIndentedLine(sb, 1, "public static bool s_create = false; // Keep AOT trimming from removing constructors");
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, "// The user created function MainApp should call this function");
        AppendIndentedLine(sb, 1, "private static void InitializeControls()");
        AppendIndentedLine(sb, 1, "{");
        AppendIndentedLine(sb, 2, "// Run static constructors to register control properties");
        if (!string.IsNullOrWhiteSpace(runStaticConstructors))
            sb.Append(runStaticConstructors).Append("\r\n");
        sb.Append("\r\n");
        AppendIndentedLine(sb, 2, "// Reister Controls");
        if (!string.IsNullOrWhiteSpace(registerControls))
            sb.Append(registerControls).Append("\r\n");
        sb.Append("\r\n");
        AppendIndentedLine(sb, 2, "// Keep AOT trimming from removing constructors, but don't actually create them");
        AppendIndentedLine(sb, 2, "if (s_create)");
        AppendIndentedLine(sb, 2, "{");
        if (!string.IsNullOrWhiteSpace(createControls))
            sb.Append(createControls).Append("\r\n");
        AppendIndentedLine(sb, 2, "}");
        if (!string.IsNullOrWhiteSpace(registerStyles))
            sb.Append(registerStyles);
        AppendIndentedLine(sb, 1, "}");
        sb.Append("}");
        return sb.ToString();
    }
}
