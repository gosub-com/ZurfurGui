using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZurfurGui.Controls;
using ZurfurGui.Layout;

namespace ZurfurGui;

/// <summary>
/// Load and build controls and components from JSON
/// </summary>
public static class Loader
{
    /// <summary>
    /// Load a JSON file into the target object.  This is the function that gets
    /// called from the InitializeComponent function in the generated code.
    /// </summary>
    public static void Load(Controllable target, string json)
    {
        var properties = LoadJsonProperties(json);
        if (target.View.Views.Count != 0)
            throw new ArgumentException("The target control already has views");
        if (target.View.Properties.Count != 0)
            throw new ArgumentException("The target control already has properties");
        if ((properties.Get(Zui.Name) ?? "") != "")
            throw new ArgumentException("Top level component properties may not be named");
        var component = properties.Get(Zui.Component) ?? "";
        if (component != target.Component)
            throw new ArgumentException($"Top level component property '{component}' must match target '{target.Component}");
        var controller = properties.Get(Zui.Controller) ?? "";
        if (controller != "")
            throw new ArgumentException($"Top level controller property '{controller}' must be empty or 'Empty'");

        target.View.Properties.SetUnion(properties);
        SetLayout(properties, target.View);
        SetDraw(properties, target.View);
        foreach (var child in properties.Get(Zui.Content) ?? Array.Empty<Properties>())
        {
            target.View.AddView(CreateControl(child).View);
        }
    }

    public static Controllable LoadJson(string json)
    {
        return CreateControl(LoadJsonProperties(json));
    }

    public static Controllable CreateControl(Properties properties)
    {
        // Create the control
        var controller = properties.Get(Zui.Controller) ?? "";
        Controllable control;
        if (controller == "")
            control = new Panel();
        else
            control = ControlManager.Create(controller)
                ?? throw new ArgumentException($"'{controller}' is not a registered control");

        // The properties become overrides, but the content becomes a parameter to LoadContent
        var contents = properties.Get(Zui.Content);
        properties.Remove(Zui.Content);
        control.View.Properties.SetUnion(properties);
        SetLayout(properties, control.View);
        SetDraw(properties, control.View);
        control.LoadContent(contents);

        return control;

    }


    private static void SetLayout(Properties properties, View view)
    {
        var layout = properties.Get(Zui.Layout) ?? "";
        if (layout != "")
        {
            switch (layout)
            {
                case "Panel": view.Layout = null; break;
                case "DockPanel": view.Layout = new LayoutDockPanel(); break;
                case "Row": view.Layout = new LayoutRow(); break;
                case "Column": view.Layout = new LayoutColumn(); break;
                default:
                    throw new ArgumentException($"The layout '{layout}' is not supported");
            }
        }
        else
        {
            if (view.Layout == null)
                view.Layout = view.Control.DefaultLayout;
        }
    }

    private static void SetDraw(Properties properties, View view)
    {
        var draw = properties.Get(Zui.Draw) ?? "";
        if (draw != "")
        {
            switch (draw)
            {
                default:
                    throw new ArgumentException($"The draw '{draw}' is not supported");
            }
        }
        else
        {
            if (view.Draw == null)
                view.Draw = view.Control.DefaultDraw;
        }
    }

    static Properties LoadJsonProperties(string json)
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
            if (name == "//")
            {
                // Ignore comments
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
