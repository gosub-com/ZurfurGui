using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiSchema
{
    /// <summary>
    /// Read the ".implements" key from a ZUI JSON document.
    /// Returns "" if not present.
    /// </summary>
    internal static string GetImplements(Dictionary<string, object?> jsonDocument)
    {
        if (jsonDocument.TryGetValue(".implements", out var v) && v is string s && !string.IsNullOrWhiteSpace(s))
            return s.Trim();
        return "";
    }

    internal static List<ZuiTypes.DataBinding> GetDataBindings(Dictionary<string, object?> jsonDocument, string typeParam = "", string typeParamConstraint = "")
    {
        if (!jsonDocument.TryGetValue(".data", out var dataSectionObj) || dataSectionObj == null)
            return new List<ZuiTypes.DataBinding>();

        if (dataSectionObj is not Dictionary<string, object?> dataSection)
            throw new Exception("The JSON '.data' key must be an object");

        var result = new List<ZuiTypes.DataBinding>();
        foreach (var kvp in dataSection)
        {
            if (kvp.Key.StartsWith("$"))
                continue; // generator-only metadata (e.g. $comment), not a binding entry

            if (kvp.Value is not Dictionary<string, object?> entry)
                throw new Exception($"The JSON '.data.{kvp.Key}' value must be an object");

            if (!entry.TryGetValue("type", out var typeObj) || typeObj is not string typeName || string.IsNullOrWhiteSpace(typeName))
                throw new Exception($"The JSON '.data.{kvp.Key}.type' must be a non-empty string");

            var binding = entry.TryGetValue("bind", out var bindingObj) && bindingObj is string bindingStr ? bindingStr : "";

            var isNullable = typeName.StartsWith("?");
            if (isNullable)
                typeName = typeName.Substring(1);

            var isCollection = typeName.StartsWith("[]");
            var isTypeParam = false;
            if (isCollection)
            {
                if (isNullable)
                    throw new Exception($"The JSON '.data.{kvp.Key}.type' must not be nullable (\"?[]\") for collection types. Use \"[]Type\" instead.");
                typeName = typeName.Substring(2);
                if (binding == "")
                    throw new Exception($"The JSON '.data.{kvp.Key}' is a collection and must have an explicit \"bind\" value (use \"data\").");
                if (binding != "data")
                    throw new Exception($"The JSON '.data.{kvp.Key}' is a collection and must use \"bind\": \"data\" (got \"{binding}\").");

                // Detect when the element type is the file's declared generic type parameter
                // (e.g. "[]Item" in "ComboBox<Item> where Item : ComboBoxItem").
                // When true, BaseType is set to the constraint name so GetBindingDataType emits
                // ObservableCollection<IComboBoxItemData> rather than ObservableCollection<IItemData>.
                if (typeParam != "" && typeName == typeParam)
                {
                    isTypeParam = true;
                    typeName = typeParamConstraint; // resolve to constraint name
                }
            }

            var comment = entry.TryGetValue("$comment", out var commentObj) && commentObj is string commentStr ? commentStr : "";

            result.Add(new ZuiTypes.DataBinding
            {
                Name = kvp.Key,
                Comment = comment,
                BaseType = NormalizeTypeName(typeName),
                Bind = binding,
                IsNullable = isNullable,
                IsCollection = isCollection,
                IsTypeParam = isTypeParam,
            });
        }

        // The .data section of the JSON is not used at runtime because we collected everything we wanted out of it.
        jsonDocument.Remove(".data");

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

    // Matches "BaseName<TypeArg>" — a closed generic controller reference in JSON
    static readonly System.Text.RegularExpressions.Regex s_closedGenericControllerRegex =
        new System.Text.RegularExpressions.Regex(@"^(\w+)<(\w+)>$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Translate a JSON controller string to the C# type name used in generated code.
    /// Plain names (e.g. "Panel") pass through unchanged.
    /// Closed generics with a control-name type arg (e.g. "ComboBox&lt;ComboBoxItemText&gt;")
    /// become "ComboBox&lt;IComboBoxItemTextData&gt;" — using the data interface convention.
    /// </summary>
    internal static string ControllerNameToCsType(string controllerName)
    {
        var m = s_closedGenericControllerRegex.Match(controllerName);
        if (m.Success)
            return $"{m.Groups[1].Value}<I{m.Groups[2].Value}Data>";
        return controllerName;
    }

    /// <summary>
    /// Recursively scan JSON control, looking for named controls.
    /// Returns a dictionary of ControlName -> C# ControlType.
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
                    controller = ControllerNameToCsType(controllerStr);
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
