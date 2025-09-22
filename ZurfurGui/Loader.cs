using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;
using ZurfurGui.Base.Serializers;
using ZurfurGui.Controls;
using ZurfurGui.Draw;
using ZurfurGui.Layout;
using ZurfurGui.Styles.Serializers;

namespace ZurfurGui;

/// <summary>
/// Load and build controls from JSON
/// </summary>
public static class Loader
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = {
                new DoublePropJsonConverter(),
                new PointPropJsonConverter(),
                new SizePropJsonConverter(),
                new ThicknessPropJsonConverter(),
                new PropertiesJsonConverter(),
                new TextLinesJsonConverter(),
                new ColorPropJsonConverter(),
                new StyleSheetJsonConverter(),
                new StylePropertyJsonConverter(),
                new JsonStringEnumConverter()
            },
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };


    /// <summary>
    /// Load a JSON file into the target object.  This is the function that gets
    /// called from the InitializeComponent function in the generated code.
    /// </summary>
    public static void Load(Controllable target, string json)
    {
        var properties = LoadJsonProperties(json);
        if (target.View.Children.Count != 0)
            throw new ArgumentException("The target control already has views");
        if (target.View.PropertiesCount != 0)
            throw new ArgumentException("The target control already has properties");
        if (properties.Get(Zui.Name) != "")
            throw new ArgumentException("Top level component properties may not be named");
        var controller = properties.Get(Zui.Controller);
        if (controller != target.TypeName)
            throw new ArgumentException($"Top level controller property '{controller}' must match target '{target.TypeName}");

        target.View.PropertiesSetUnionInternal(properties);
        SetLayout(properties, target.View);
        foreach (var child in properties.Get(Zui.Content))
        {
            target.View.AddChild(CreateControl(child).View);
        }
    }

    public static Controllable LoadJson(string json)
    {
        return CreateControl(LoadJsonProperties(json));
    }

    static Properties LoadJsonProperties(string json)
    {
        var properties = JsonSerializer.Deserialize<Properties>(json, JsonSerializerOptions);
        return properties ?? throw new Exception("Invalid properties JSON");
    }

    public static Controllable CreateControl(Properties properties)
    {
        // Create the control
        var controller = properties.Get(Zui.Controller);
        Controllable control;
        if (controller == "")
            control = new Panel();
        else
            control = ControlManager.Create(controller)
                ?? throw new ArgumentException($"'{controller}' is not a registered control");

        // The properties become overrides, but the content becomes a parameter to LoadContent
        var contents = properties.Get(Zui.Content);
        properties.Remove(Zui.Content);
        control.View.PropertiesSetUnionInternal(properties);
        SetLayout(properties, control.View);
        control.LoadContent(contents);

        return control;

    }


    private static void SetLayout(Properties properties, View view)
    {
        var layout = properties.Get(Zui.Layout);
        if (layout != "")
        {
            switch (layout)
            {
                case "Panel": view.Layout = null; break;
                case "DockPanel": view.Layout = new LayoutDockPanel(); break;
                case "Row": view.Layout = new LayoutRow(); break;
                case "Column": view.Layout = new LayoutColumn(); break;
                case "Text": view.Layout = new LayoutText(); break;
                default:
                    throw new ArgumentException($"The layout '{layout}' is not supported");
            }
        }
    }


}
