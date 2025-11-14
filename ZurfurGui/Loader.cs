using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Layout;
using ZurfurGui.Property;
using ZurfurGui.Property.Serializers;
using ZurfurGui.Windows;

namespace ZurfurGui;

// Source-generated JSON context
[JsonSerializable(typeof(Properties))]
[JsonSerializable(typeof(Properties[]))]
[JsonSerializable(typeof(AlignProp))]
[JsonSerializable(typeof(TextLines))]
[JsonSerializable(typeof(TextLinesProp))]
[JsonSerializable(typeof(FontProp))]
[JsonSerializable(typeof(ColorProp))]
[JsonSerializable(typeof(SizeProp))]
[JsonSerializable(typeof(ThicknessProp))]
[JsonSerializable(typeof(PointProp))]
[JsonSerializable(typeof(DoubleProp))]
[JsonSerializable(typeof(EnumProp<bool>))]
[JsonSerializable(typeof(EnumProp<Dock>))]
[JsonSerializable(typeof(StyleSheet))]
[JsonSerializable(typeof(string[]))]
public partial class ZurfurJsonContext : JsonSerializerContext { }

/// <summary>
/// Load and build controls from JSON
/// </summary>
public static class Loader
{
    static Dictionary<string, Type> s_controllers = new();
    static Dictionary<string, Func<Layoutable?>> s_layouts = new();
    static Dictionary<string, StyleSheet> s_styleSheets = new();

    // Combine source-generated context with custom converters
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = ZurfurJsonContext.Default, // Use source-generated context
        Converters = {
            // Add custom converters
            new PropertiesJsonConverter(),
            new EnumPropJsonConverter<bool>(),
            new EnumPropJsonConverter<Dock>(),
            new DoublePropJsonConverter(),
            new TextLinesJsonConverter(),
            new TextLinesPropJsonConverter(),
            new ColorPropJsonConverter(),
            new JsonStringEnumConverter()
        },
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static AppWindow Init(Action<AppWindow> mainAppEntry)
    {
        _ = Zui.Name; // Force initialization of static properties
        ZurfurMain.MainApp();
        RegisterLayout("Panel", () => null);
        RegisterLayout("DockPanel", () => new LayoutDockPanel());
        RegisterLayout("Row", () => new LayoutRow());
        RegisterLayout("Column", () => new LayoutColumn());
        RegisterLayout("Text", () => LayoutText.Instance);

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }


    /// <summary>
    /// Load a JSON file into the target object. This is the function that gets
    /// called from the InitializeComponent function in the generated code.
    /// </summary>
    public static void Load(Controllable target, string json)
    {
        try
        {
            var properties = LoadJsonProperties(json);
            if (target.View.Children.Count != 0)
                throw new ArgumentException("The target control already has views");
            if (target.View.PropertiesCount != 0)
                throw new ArgumentException("The target control already has properties");
            if (properties.Get(Zui.Name) != null)
                throw new ArgumentException("Top level component properties may not be named");
            var controller = properties.Get(Zui.Controller) ?? "";
            if (controller != target.TypeName)
                throw new ArgumentException($"Top level controller property '{controller}' must match target '{target.TypeName}");

            target.View.PropertiesSetUnionInternal(properties);
            SetLayout(properties, target.View);
            foreach (var child in properties.Get(Zui.Content) ?? [])
            {
                target.View.AddChild(CreateControl(child).View);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading control '{target.TypeName}': {ex.Message}, type={ex.GetType()}");
            throw;
        }
    }

    public static void RegisterStyleSheet(string json) 
    { 
        var styleSheet = JsonSerializer.Deserialize<StyleSheet>(json, JsonSerializerOptions);
        if (styleSheet == null || styleSheet.Name == "")
            throw new ArgumentException("Invalid style sheet, missing name");
        if (s_styleSheets.ContainsKey(styleSheet.Name))
            throw new ArgumentException($"Style sheet '{styleSheet.Name}' is already registered");
        s_styleSheets[styleSheet.Name] = styleSheet;
    }

    public static StyleSheet? GetStyleSheet(string name)
    {
        if (s_styleSheets.TryGetValue(name, out var styleSheet))
            return styleSheet;
        return null;
    }

    public static void RegisterControl(string name, Type type)
    {
        // Check if the name is already registered
        if (s_controllers.TryGetValue(name, out var existingType))
        {
            if (existingType == type)
                return; // Already registered
            throw new ArgumentException($"Control '{name}' is already registered for type '{existingType.Name}'");
        }

        // Verify that the type implements the Controllable interface
        if (!typeof(Controllable).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type '{type.Name}' does not implement the Controllable interface.");
        }

        s_controllers[name] = type;
    }

    public static void RegisterLayout(string name, Func<Layoutable?> layoutFactory)
    {
        if (s_layouts.ContainsKey(name))
            throw new ArgumentException($"Layout '{name}' is already registered");
        s_layouts[name] = layoutFactory;
    }

    static Properties LoadJsonProperties(string json)
    {
        var properties = JsonSerializer.Deserialize<Properties>(json, JsonSerializerOptions);
        return properties ?? throw new Exception("Invalid or null properties JSON");
    }

    /// <summary>
    /// Create a control from the given properties
    /// </summary>
    public static Controllable CreateControl(Properties properties)
    {
        // Create the control
        var controller = properties.Get(Zui.Controller) ?? "";
        Controllable control;
        if (controller == "")
        {
            control = new Panel();
        }
        else
        {
            if (!s_controllers.TryGetValue(controller, out var type))
                throw new ArgumentException($"'{controller}' is not a registered control: "
                    + $"{string.Join(",\r\n", s_controllers.Keys)}");

            control = (Controllable?)Activator.CreateInstance(type)
                ?? throw new ArgumentException($"Could not create instance of '{controller}'");
        }

        // The properties become overrides, but the content becomes a parameter to LoadContent
        var contents = properties.Get(Zui.Content) ?? [];
        properties.Remove(Zui.Content);
        control.View.PropertiesSetUnionInternal(properties);
        SetLayout(properties, control.View);
        control.LoadContent(contents);

        return control;
    }

    private static void SetLayout(Properties properties, View view)
    {
        var layout = properties.Get(Zui.Layout) ?? "";
        if (layout != "")
        {
            if (s_layouts.TryGetValue(layout, out var createFunc))
                view.Layout = createFunc();
            else
                throw new ArgumentException($"The layout '{layout}' is not supported");
        }
    }
}

