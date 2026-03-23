using System;
using System.Collections.Generic;
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
        sb.Append("using ZurfurGui.Controls;\r\n\r\n");

        if (data.Use != null && data.Use.Count != 0)
            foreach (var u in data.Use)
                sb.Append($"using {u};\r\n");

        sb.Append("\r\n");

        return sb.ToString();
    }

    internal static string GenerateDataContractInterfaceSource(ZuiTypes.FileInfo data)
    {
        if (data.Bindings.Count == 0)
            return "";

        var interfaceName = $"I{data.FileName}Data";

        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public interface {interfaceName} : INotifyPropertyChanged\r\n{{\r\n");
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"{p.Type} {p.Name} {{ get; set; }}");
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
                continue;
            var bindingPath = binding.Bind.Split('.');
            var bindingName = bindingPath[0];

            if (!namedControls.ContainsKey(bindingName))
            {
                throw new Exception($"The binding for '{binding.Name}' does not match a valid control name. Binding: '{binding.Bind}'");
            }

        }

        var interfaceName = $"I{data.FileName}Data";
        var className = $"{data.FileName}Data";

        var partialKeyword = data.UserSuppliedDataClass ? "partial " : "";

        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public sealed {partialKeyword}class {className} : {interfaceName}\r\n{{\r\n");

        // Generate static PropertyChangedEventArgs for each property
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"static readonly PropertyChangedEventArgs s_{ToCamelCase(p.Name)}EventArgs = new(nameof({p.Name}));");
        sb.Append("\r\n");

        // Generate backing fields
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"{p.Type} __{ToCamelCase(p.Name)};");
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
            var backingField = $"__{ToCamelCase(p.Name)}";
            var eventArgsField = $"s_{ToCamelCase(p.Name)}EventArgs";
            AppendIndentedLine(sb, 1, $"public required {p.Type} {p.Name}");
            AppendIndentedLine(sb, 1, "{");
            AppendIndentedLine(sb, 2, $"get => {backingField};");
            AppendIndentedLine(sb, 2, "set");
            AppendIndentedLine(sb, 2, "{");
            AppendIndentedLine(sb, 3, $"if (!EqualityComparer<{p.Type}>.Default.Equals({backingField}, value))");
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

    static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

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

        // Data bindings
        if (data.Bindings.Count != 0)
            AppendIndentedLine(sb, 1, $"public I{data.ControllerName}Data DataContext {{ get; set; }} = null!;");
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
            AppendIndentedLine(sb, 2, $"return new {data.ControllerName}Data");
            AppendIndentedLine(sb, 2, "{");

            var initLines = new List<string>();
            foreach (var binding in data.Bindings)
            {
                var initCode = GenerateDataInitializationCode(binding, namedControlsDict);
                if (!string.IsNullOrEmpty(initCode))
                    initLines.Add(initCode);
            }

            for (int i = 0; i < initLines.Count; i++)
            {
                var line = initLines[i];
                var suffix = i < initLines.Count - 1 ? "," : "";
                AppendIndentedLine(sb, 3, line + suffix);
            }

            AppendIndentedLine(sb, 2, "};");
            AppendIndentedLine(sb, 1, "}");
        }

        // Access to JSON content
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");
        sb.Append("}");
        return sb.ToString();
    }

    static string GenerateDataInitializationCode(ZuiTypes.DataBinding binding, Dictionary<string, string> namedControls)
    {        
        // If this binds directly to a named control, use the data context
        if (namedControls.ContainsKey(binding.Bind))
        {
            return $"{binding.Name} = {binding.Bind}.DataContext";
        }

        // For all other types, initialize with new instance
        return $"{binding.Name} = new {binding.Type}()";
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
