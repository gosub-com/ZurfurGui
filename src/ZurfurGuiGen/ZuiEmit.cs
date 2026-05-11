using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmit
{
    internal static void AppendFileHeader(StringBuilder sb, string sourceFileName)
    {
        sb.Append($"// This file is generated from '{sourceFileName}' on {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n");
    }

    internal static void AppendIndentedLine(this StringBuilder sb, int indentLevel, string line)
    {
        for (var i = 0; i < indentLevel; i++)
            sb.Append("    ");
        sb.Append(line);
        sb.Append("\r\n");
    }

    /// <summary>
    /// Appends a /// &lt;summary&gt; XML doc comment block for the given text.
    /// The text is XML-escaped so that any &amp;, &lt;, &gt;, or " characters are safe in XML.
    /// </summary>
    internal static void AppendXmlDocComment(StringBuilder sb, int indentLevel, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return;
        var escaped = comment
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
        sb.AppendIndentedLine(indentLevel, "/// <summary>");
        sb.AppendIndentedLine(indentLevel, $"/// {escaped}");
        sb.AppendIndentedLine(indentLevel, "/// </summary>");
    }

    internal static string GenerateUsingCode(ZuiTypes.FileInfo data)
    {
        var sb = new StringBuilder();
        sb.Append("using System.Collections.ObjectModel;\r\n");
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

    internal static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]))
            return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    internal static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Returns the C# data type for a binding.
    /// For type-param collections (IsTypeParam), returns ObservableCollection&lt;I{Constraint}Data&gt;
    /// where Constraint is the control named in the "where" clause — keeping the data layer non-generic.
    /// For regular collections (IsCollection), returns ObservableCollection&lt;I{type}Data&gt;.
    /// </summary>
    internal static string GetBindingDataType(ZuiTypes.DataBinding binding, Dictionary<string, string> namedControls)
    {
        if (binding.IsTypeParam)
            // Type param collection: element type is the constraint's data interface, not the type param itself.
            // e.g. "[]Item where Item : ComboBoxItem" -> ObservableCollection<IComboBoxItemData>
            return $"global::System.Collections.ObjectModel.ObservableCollection<I{binding.BaseType}Data>";

        if (binding.IsCollection)
            return $"global::System.Collections.ObjectModel.ObservableCollection<I{binding.BaseType}Data>";

        // If the binding targets a named control itself (e.g. "bind": "_card1"), the data-binding
        // type should be that control's data contract (I<ControlName>Data).
        if (IsNamedControl(binding.Bind, namedControls))
        {
            var controlTypeName = namedControls[binding.Bind];
            return $"I{controlTypeName}Data";
        }

        return binding.NullableType;
    }

    internal static bool IsNamedControl(string bind, Dictionary<string, string> namedControls)
        => namedControls.ContainsKey(bind);
}
