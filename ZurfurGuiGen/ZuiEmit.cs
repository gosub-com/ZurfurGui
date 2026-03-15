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

        sb.Append($"public interface {interfaceName}\r\n{{\r\n");
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"{p.Type} {p.Name} {{ get; set; }}");
        sb.Append("}");
        return sb.ToString();
    }

    internal static string GenerateDataImplementationSource(ZuiTypes.FileInfo data)
    {
        if (data.Bindings.Count == 0)
            return "";

        var interfaceName = $"I{data.FileName}Data";
        var className = $"{data.FileName}Data";

        var partialKeyword = data.UserSuppliedDataClass ? "partial " : "";

        var sb = new StringBuilder();
        AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(GenerateUsingCode(data));
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public {partialKeyword}class {className} : {interfaceName}\r\n{{\r\n");
        foreach (var p in data.Bindings)
            AppendIndentedLine(sb, 1, $"public {p.Type} {p.Name} {{ get; set; }}");
        sb.Append("}");
        return sb.ToString();
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
            AppendIndentedLine(sb, 1, $"public I{data.ControllerName}Data DataContext = new {data.ControllerName}Data();");
        sb.Append("\r\n");

        // Create named control variables
        var namedControls = ZuiSchema.FindNamedControls(data.JsonDocument).OrderBy(c => c.ControlName);
        var namedControlsCode = string.Join("\r\n", namedControls.Select(a =>
        {
            var qualifier = a.ControlName.StartsWith("_") ? "private" : "public ";
            return $"    {qualifier} {a.ControlType} {a.ControlName};";
        }));

        // Add named control variables
        AppendIndentedLine(sb, 1, "// Named controls (public unless name starts with '_')");
        if (!string.IsNullOrWhiteSpace(namedControlsCode))
            sb.Append(namedControlsCode).Append("\r\n");

        // Add constructor
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

        // Create InitializeControl code to initialize named controls
        var initControlsCode = string.Join("\r\n", namedControls.Select(a =>
        {
            var qualifier = a.ControlName.StartsWith("_") ? "private" : "public ";
            return $"        {a.ControlName} = ({a.ControlType})View.FindByName(\"{a.ControlName}\").Controller;";
        }));

        // Add InitializeControl code to initialize named controls
        AppendIndentedLine(sb, 2, "// Initialize named controls");
        if (!string.IsNullOrWhiteSpace(initControlsCode))
            sb.Append(initControlsCode).Append("\r\n");
        AppendIndentedLine(sb, 1, "}");

        // Access to JSON content
        sb.Append("\r\n");
        AppendIndentedLine(sb, 1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");
        sb.Append("}");
        return sb.ToString();
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
