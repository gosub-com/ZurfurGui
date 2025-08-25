using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui;

public class ZurfurJsonLoader
{
    /// <summary>
    /// Load a JSON file into the target object, adding the views and returning the properties.
    /// </summary>
    public static Properties Load(Controllable target, string json)
    {
        var jsonDoc = JsonDocument.Parse(json);
        var properties = GetPropertiesObject(jsonDoc.RootElement);
        target.View.AddView(Helper.BuildView(properties));
        return properties;
    }

    static Properties GetPropertiesObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Expected a JSON object", nameof(element));
        var properties = new Properties();
        foreach (var e in element.EnumerateObject())
        {
            var name = e.Name;
            if (name == "Type")
                continue;

            var info = PropertyKeys.GetInfo(name);
            if (info == null)
                throw new Exception($"Unknown property name: '{name}'");

            if (info.Type == typeof(string))
            {
                properties.SetById(info.Id, e.Value.GetString() ?? "");
            }
            else if (info.Type == typeof(Properties[]))
            {
                properties.SetById(info.Id, GetPropertiesArray(e.Value));
            }
            else if (info.Type.IsEnum)
            {
                var enumType = info.Type;
                var enumName = e.Value.GetString() ?? "";
                if (!Enum.TryParse(enumType, enumName, true, out var enumValue))
                    throw new Exception($"The property '{info.Name}' with type '{enumType}' has an unknown enum value '{enumName}'");
                properties.SetById(info.Id, enumValue);

            }
            else if (info.Type.IsValueType)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    };
                    var infoObject = e.Value.Deserialize(info.Type, options) ?? throw new Exception("Null not allowed");
                    properties.SetById(info.Id, infoObject);
                }
                catch (Exception ex)
                {
                    throw new Exception($"The property '{info.Name}' with type '{info.Type}' is not a valid JSON object: {ex.Message}");
                }
            }
            else
            {
                throw new Exception($"The property '{info.Name}' has an unsupported type '{info.Type}'");
            }
        }

        return properties;
    }

    static Properties[] GetPropertiesArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Expected a JSON array", nameof(element));
        var list = new List<Properties>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Expected a JSON object in the array", nameof(item));
            list.Add(GetPropertiesObject(item));
        }
        return list.ToArray();
    }


}
