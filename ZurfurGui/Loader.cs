using System.Reflection;
using System.Runtime.CompilerServices;
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
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
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
[JsonSerializable(typeof(EnumProp<DockEnum>))]
[JsonSerializable(typeof(StyleSheet))]
[JsonSerializable(typeof(DataBinding))]
[JsonSerializable(typeof(Dictionary<string,DataBinding>))]
[JsonSerializable(typeof(string[]))]
public partial class ZurfurJsonContext : JsonSerializerContext { }


/// <summary>
/// Load and build controls from JSON
/// </summary>
public static class Loader
{
    public readonly record struct ControlCreationContext(
        string TypeName,
        string TypeNamespace,
        TextLines TypeUses)
    {
        public static ControlCreationContext From(Controllable control)
            => new(control.TypeName, control.TypeNamespace, control.TypeUses);
    }

    static Dictionary<string, Type> s_controllers = new();
    static Dictionary<string, Func<Layoutable?>> s_layouts = new();
    static Dictionary<string, StyleSheet> s_styleSheets = new();

    // Combine source-generated context with custom converters
    public static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = ZurfurJsonContext.Default,
        Converters = {
            // Add custom converters
            new PropertiesJsonConverter(),
            new EnumPropJsonConverter<bool>(),
            new EnumPropJsonConverter<DockEnum>(),
            new DoublePropJsonConverter(),
            new TextLinesJsonConverter(),
            new TextLinesPropJsonConverter(),
            new ColorPropJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    public static AppWindow Init(Action<AppWindow> mainAppEntry)
    {
        RuntimeHelpers.RunClassConstructor(typeof(LayoutRow).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(LayoutDock).TypeHandle);

        RegisterLayout("Panel", () => null);
        RegisterLayout("Dock", () => new LayoutDock());
        RegisterLayout("Row", () => new LayoutRow());
        RegisterLayout("Column", () => new LayoutColumn());
        RegisterLayout("Text", () => LayoutText.Instance);

        ZurfurMain.MainApp();

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }


    /// <summary>
    /// Load a JSON file into the target object. This is the function that gets
    /// called from the InitializeControl function in the generated code.
    /// </summary>
    public static void Load(Controllable target, string json)
    {
        try
        {
            var properties = LoadJsonProperties(json);
            if (target.View.Children.Count != 0)
                throw new ArgumentException($"The target control '{target.TypeName}' already has views");
            if (target.View.PropertiesCount != 0)
                throw new ArgumentException($"The target control '{target.TypeName}' already has properties");
            if (properties.Get(Panel.Name) != null)
                throw new ArgumentException($"Top level component properties of '{target.TypeName}' may not be named");
            var controller = properties.Get(Panel.Controller) ?? "";
            if (controller != target.TypeName)
                throw new ArgumentException($"Top level controller property '{controller}' must match target '{target.TypeName}");

            BuildContent(target, properties, ControlCreationContext.From(target));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading control '{target.TypeName}': {ex.Message}, type={ex.GetType()}");
            throw;
        }
    }

    private static void BuildContent(Controllable control, Properties properties, ControlCreationContext context)
    {
        // The properties become overrides, but the content becomes a parameter to LoadContent
        // TBD: Maybe don't send content as parameter here (let LoadContent do it)?
        var content = properties.Get(Panel.Content);
        properties.Remove(Panel.Content);
        control.View.PropertiesSetUnionInternal(properties);
        SetLayout(properties, control.View);
        control.LoadContent(content, context);
    }

    public static void RegisterStyleSheet(string json) 
    {
        var styleSheet = JsonSerializer.Deserialize<StyleSheet>(json, s_jsonSerializerOptions);
        if (styleSheet == null || styleSheet.Name ==  null || styleSheet.Name == "")
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
        var properties = JsonSerializer.Deserialize<Properties>(json, s_jsonSerializerOptions);
        return properties ?? throw new Exception("Invalid or null properties JSON");
    }

    /// <summary>
    /// Create a control from the given properties with a parent/type context.
    /// (Context is not yet used for controller resolution; it is threaded through for future use.)
    /// </summary>
    public static Controllable CreateControl(Properties properties, ControlCreationContext context)
    {
        // Create the control
        var controller = properties.Get(Panel.Controller) ?? "";
        var type = FindControllerType(controller, context);
        Controllable control;
        try
        {
            control = (Controllable?)Activator.CreateInstance(type)
                ?? throw new ArgumentException($"Could not create instance of '{controller}'");
        }
        catch (TargetInvocationException tex)
        {
            if (tex.InnerException != null)
                throw tex.InnerException;
            throw;
        }

        BuildContent(control, properties, context);

        return control;
    }

    static Type FindControllerType(string controller, ControlCreationContext context)
    {
        // Use Panel if controller is not specified
        if (controller == "")
            return typeof(Panel); // Default to panel when no controller specified

        // Check fully qualified name
        if (s_controllers.TryGetValue(controller, out var type))
            return type;

        // Use namespace
        if (s_controllers.TryGetValue($"{context.TypeNamespace}.{controller}", out type))
            return type;

        // Check uses
        foreach (var use in context.TypeUses)
            if (s_controllers.TryGetValue($"{use}.{controller}", out type))
                return type;

        // Check base library
        if (s_controllers.TryGetValue($"ZurfurGui.Controls.{controller}", out type))
            return type;

        throw new ArgumentException($"'{controller}' is not a registered control: "
            + $"{string.Join(",\r\n", s_controllers.Keys)}");
    }

    private static void SetLayout(Properties properties, View view)
    {
        var layout = properties.Get(Panel.Layout) ?? "";
        if (layout != "")
        {
            if (s_layouts.TryGetValue(layout, out var createFunc))
                view.Layout = createFunc();
            else
                throw new ArgumentException($"The layout '{layout}' is not supported");
        }
    }
}

