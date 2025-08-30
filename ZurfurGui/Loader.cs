using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZurfurGui.Controls;

namespace ZurfurGui;

/// <summary>
/// Load and build controls and components from JSON
/// 
/// TBD: Split out the layout from the controller
/// </summary>
public static class Loader
{
    /// <summary>
    /// Load a JSON file into the target object, adding the views and returning the properties.
    /// </summary>
    public static void Load(Controllable target, string json)
    {
        if (target.View.Views.Count != 0)
            throw new ArgumentException("The target control already has views");
        if (target.View.Properties.Count != 0)
            throw new ArgumentException("The target control already has properties");

        var properties = LoadJsonProperties(json);
        if ((properties.Get(Zui.Name) ?? "") != "")
            throw new ArgumentException("Top level component properties may not be named");

        target.View.Properties.SetUnion(properties);
        if ((properties.Get(Zui.Controller) ?? "") != "Empty")
            target.View.AddView(BuildView(properties));
    }


    /// <summary>
    /// Create a view from property
    /// </summary>
    public static View BuildView(Properties properties)
    {
        var controlName = properties.Get(Zui.Controller) ?? "";
        if (controlName == "")
            throw new ArgumentException($"The control's controller property is not specified.");

        var control = ControlRegistry.Create(controlName);
        if (control == null)
            throw new ArgumentException($"'{controlName}' is not a registered control");

        // TBD: don't allow changing component or controller names here.
        //      Need to split out the layout from the Controller

        //var oldComponentName = control.View.Properties.Get(Zui.Component) ?? "";
        //var oldControllerName = control.View.Properties.Get(Zui.Controller) ?? "";

        control.View.Properties.SetUnion(properties);
        
        //var newComponentName = control.View.Properties.Get(Zui.Component) ?? "";
        //var newControllerName = control.View.Properties.Get(Zui.Controller) ?? "";
        //if (oldComponentName != newComponentName)
        //    throw new ArgumentException($"Component name changed from '{oldComponentName}' to '{newComponentName}'");
        //if (oldControllerName != newControllerName)
        //    throw new ArgumentException($"Component name changed from '{oldControllerName}' to '{newControllerName}'");

        control.LoadContent();

        return control.View;
    }


    public static Controllable LoadJson(string json)
    {
        var properties = LoadJsonProperties(json);
        return BuildView(properties).Control;
    }

    /// <summary>
    /// Build views from properties and add them to the view.Views collection
    /// </summary>
    public static void BuildViews(View view, Properties[]? controllerProperties)
    {
        if (controllerProperties == null)
            return;

        foreach (var property in controllerProperties)
            view.AddView(BuildView(property));
    }



    public static Properties LoadJsonProperties(string json)
    {
        return LoadJsonProperties(JsonDocument.Parse(json).RootElement);
    }

    static Properties LoadJsonProperties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Expected a JSON object", nameof(element));
        var properties = new Properties();
        foreach (var e in element.EnumerateObject())
        {
            var name = e.Name;
            if (name == "Type")
            {
                properties.Set(Zui.Component, e.Value.GetString() ?? "");
                continue;
            }

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
            else if (info.Type == typeof(Color))
            {
                // Parse a css color string
                if (e.Value.ValueKind != JsonValueKind.String)
                    throw new Exception($"The property '{info.Name}' must be a string containing a CSS color");
                var colorString = e.Value.ToString();
                if (Color.ParseCss(colorString) is not Color color)
                    throw new Exception($"The property '{info.Name}' with value '{colorString}' is not a valid CSS color");
                properties.SetById(info.Id, color);
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
            list.Add(LoadJsonProperties(item));
        }
        return list.ToArray();
    }

}
