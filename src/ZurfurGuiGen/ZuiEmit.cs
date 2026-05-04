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

    internal static string GenerateUsingCode(ZuiTypes.FileInfo data)
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

    internal static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]))
            return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// Use either the type name, or I{type}Data if it is a control
    /// </summary>
    internal static string GetBindingDataType(ZuiTypes.DataBinding binding, Dictionary<string, string> namedControls)
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

    internal static bool IsNamedControl(string bind, Dictionary<string, string> namedControls)
        => namedControls.ContainsKey(bind);
}
