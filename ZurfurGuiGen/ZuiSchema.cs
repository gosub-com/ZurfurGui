using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiSchema
{
    internal static List<ZuiTypes.DataBinding> GetDataBindings(Dictionary<string, object?> jsonDocument)
    {
        if (!jsonDocument.TryGetValue(".data", out var dataSectionObj) || dataSectionObj == null)
            return new List<ZuiTypes.DataBinding>();

        if (dataSectionObj is not Dictionary<string, object?> dataSection)
            throw new Exception("The JSON '.data' key must be an object");

        var result = new List<ZuiTypes.DataBinding>();
        foreach (var kvp in dataSection)
        {
            if (kvp.Value is not Dictionary<string, object?> entry)
                throw new Exception($"The JSON '.data.{kvp.Key}' value must be an object");

            if (!entry.TryGetValue("type", out var typeObj) || typeObj is not string typeName || string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"The JSON '.data.{kvp.Key}.type' must be a non-empty string");

            var binding = entry.TryGetValue("bind", out var bindingObj) && bindingObj is string bindingStr ? bindingStr : "";

            result.Add(new ZuiTypes.DataBinding { Name = ToPascalIdentifier(kvp.Key), Type = NormalizeTypeName(typeName), Bind = binding });
        }

        return result;
    }

    internal static string NormalizeTypeName(string typeName)
    {
        // Allow simple aliases in JSON
        switch (typeName)
        {
            case "Bool":
            case "bool":
                return "bool";
            case "Int":
            case "int":
                return "int";
            case "Long":
            case "long":
                return "long";
            case "Double":
            case "double":
                return "double";
            case "String":
            case "string":
                return "string";
            default:
                return typeName;
        }
    }

    internal static string ToPascalIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "_";

        // Convert common schema keys like "text" or "isChecked" into PascalCase property names.
        // Keep underscores as word separators.
        var sb = new StringBuilder(name.Length);
        var makeUpper = true;
        foreach (var ch in name)
        {
            if (ch == '_' || ch == '-' || ch == ' ')
            {
                makeUpper = true;
                continue;
            }

            if (sb.Length == 0)
            {
                sb.Append(char.IsLetter(ch) ? char.ToUpperInvariant(ch) : '_');
                makeUpper = false;
                continue;
            }

            sb.Append(makeUpper ? char.ToUpperInvariant(ch) : ch);
            makeUpper = false;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Recursively scan JSON control, looking for named controls.
    /// Returns a dictionary of ControlName -> ControlType.
    /// Throws if duplicate ControlName is detected.
    /// </summary>
    internal static Dictionary<string, string> FindNamedControlsDictionary(Dictionary<string, object?> json)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        ScanJson(json);
        return result;

        // Helper function to recursively scan the JSON
        void ScanJson(Dictionary<string, object?> currentJson)
        {
            if (currentJson.TryGetValue(".name", out var controlNameObj)
                && controlNameObj is string controlName)
            {
                var controller = "Panel";
                if (currentJson.TryGetValue(".controller", out var controllerObj) && controllerObj is string controllerStr)
                {
                    controller = controllerStr;
                }

                if (result.ContainsKey(controlName))
                    throw new Exception($"Duplicate control name '{controlName}' detected");

                result.Add(controlName, controller);
            }

            // Recursively scan nested dictionaries or arrays
            foreach (var value in currentJson.Values)
            {
                if (value is Dictionary<string, object?> nestedDict)
                {
                    ScanJson(nestedDict);
                }
                else if (value is List<object?> nestedList)
                {
                    foreach (var item in nestedList)
                    {
                        if (item is Dictionary<string, object?> listItemDict)
                        {
                            ScanJson(listItemDict);
                        }
                    }
                }
            }
        }

    }
}
