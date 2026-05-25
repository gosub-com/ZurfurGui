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
[JsonSerializable(typeof(FontProp))]
[JsonSerializable(typeof(SizeProp))]
[JsonSerializable(typeof(ThicknessProp))]
[JsonSerializable(typeof(PointProp))]
[JsonSerializable(typeof(DoubleProp))]
[JsonSerializable(typeof(StyleSheet))]
[JsonSerializable(typeof(ThemeSheet))]
[JsonSerializable(typeof(Dictionary<string,JsonElement>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(DockEnum))]
[JsonSerializable(typeof(Color))]
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

    record struct ControlEntry(Type Type, Func<Controllable> Factory);

    static Dictionary<string, ControlEntry> s_controllers = new();
    static Dictionary<string, Func<Layoutable?>> s_layouts = new();

    // Maps item data interface type → factory; built automatically in RegisterControl
    // from each control's ImplementsDataInterfaces property.
    static Dictionary<Type, Func<Controllable>> s_dataControllers = new();

    // Combine source-generated context with custom converters
    static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = ZurfurJsonContext.Default,
        Converters = {
            // Add custom converters
            new PropertiesJsonConverter(),
            new DoublePropJsonConverter(),
            new TextLinesJsonConverter(),
            new ColorJsonConverter(),
            new ThicknessPropJsonConverter(),
            new FontPropJsonConverter(),
            new PointPropJsonConverter(),
            new SizePropJsonConverter(),
            new AlignPropJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions;

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

            // All of the following checks are enforced by the code generator
            if (target.View.Children.Count != 0)
                throw new ArgumentException($"The target control '{target.TypeName}' already has views");
            if (target.View.PropertiesCount != 0)
                throw new ArgumentException($"The target control '{target.TypeName}' already has properties");
            if (properties.Get(Panel.Name) != null)
                throw new ArgumentException($"Top level component properties of '{target.TypeName}' may not be named");
            var controller = properties.Get(Panel.Controller) ?? "";
            var controllerBaseName = controller.Contains('<') ? controller.Substring(0, controller.IndexOf('<')) : controller;
            if (controllerBaseName != target.TypeName)
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

    /// <summary>
    /// Apply data properties from JSON to the control's DataContext, recursively processing
    /// children first, then applying parent properties. This deserializes unknown properties 
    /// stored in Panel.DataProperties and applies them via the control's SetDataProperty method.
    /// Converts JSON camelCase names to PascalCase for the C# API.
    /// Children are processed first so parent bindings can override child defaults.
    /// Should be called after DataContext is initialized.
    /// </summary>
    public static void ApplyDataProperties(Controllable control)
    {
        // Recursively apply to all child controls
        foreach (var childView in control.View.Children)
        {
            ApplyDataProperties(childView.Controller);
        }

        // Apply data properties to this control (after children)
        var properties = control.View._properties;
        var dataProperties = properties.Get(Panel.DataProperties);

        if (dataProperties != null && dataProperties.Count > 0)
        {
            var dataPropertyInfo = control.DataPropertyInfo;

            foreach (var (jsonPropertyName, jsonElement) in dataProperties)
            {
                // Convert JSON camelCase to PascalCase for lookup and API
                var pascalCaseName = ToPascalCase(jsonPropertyName);

                // Validate property exists in DataPropertyInfo (uses PascalCase keys)
                if (!dataPropertyInfo.TryGetValue(pascalCaseName, out var propInfo))
                {
                    throw new InvalidOperationException(
                        $"Data property '{jsonPropertyName}' is not declared in control '{control.TypeName}'. " +
                        $"Available properties: {string.Join(", ", dataPropertyInfo.Keys)}");
                }

                // Deserialize JsonElement to the expected type
                object? value;
                try
                {
                    value = JsonSerializer.Deserialize(jsonElement.GetRawText(), propInfo.BaseType, s_jsonSerializerOptions);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize data property '{jsonPropertyName}' to type '{propInfo.BaseType.Name}' " +
                        $"in control '{control.TypeName}': {ex.Message}", 
                        ex);
                }

                // Validate nullability
                if (value == null && !propInfo.IsNullable)
                {
                    throw new InvalidOperationException(
                        $"Data property '{jsonPropertyName}' cannot be null (type: {propInfo.BaseType.Name}) " +
                        $"in control '{control.TypeName}'");
                }

                // Apply via SetDataProperty (expects PascalCase)
                if (!control.SetDataProperty(pascalCaseName, value))
                {
                    throw new InvalidOperationException(
                        $"Failed to set data property '{jsonPropertyName}' (as '{pascalCaseName}') on control '{control.TypeName}'. " +
                        $"SetDataProperty returned false.");
                }
            }
        }
    }

    /// <summary>
    /// Convert camelCase to PascalCase
    /// </summary>
    private static string ToPascalCase(string camelCase)
    {
        if (string.IsNullOrEmpty(camelCase) || char.IsUpper(camelCase[0]))
            return camelCase;
        return char.ToUpper(camelCase[0]) + camelCase.Substring(1);
    }


    /// <summary>
    /// <summary>
    /// Register a control by name with its type and factory.
    /// The factory is used by CreateControl.
    /// </summary>
    public static void RegisterControl(string name, Type type, Func<Controllable> factory)
    {
        if (s_controllers.TryGetValue(name, out var existing))
        {
            if (existing.Type == type)
                return; // Already registered
            throw new ArgumentException($"Control '{name}' is already registered for type '{existing.Type.Name}'");
        }
        if (!typeof(Controllable).IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.Name}' does not implement the Controllable interface.");

        s_controllers[name] = new ControlEntry(type, factory);
    }

    /// <summary>
    /// Register a control and wire up its data-controller mappings from its static
    /// ImplementsDataInterfaces array. Eliminates the need for a separate RegisterDataController call.
    /// </summary>
    public static void RegisterControl(string name, Type type, Func<Controllable> factory,
        Type[] implementsDataInterfaces)
    {
        RegisterControl(name, type, factory);
        foreach (var iface in implementsDataInterfaces)
        {
            if (!s_dataControllers.ContainsKey(iface))
                s_dataControllers[iface] = factory;
        }
    }

    /// <summary>
    /// Get the factory for the item controller that handles the given item data interface type.
    /// Accepts either the registered interface type or a concrete class that implements it.
    /// </summary>
    public static Func<Controllable> GetDataControllerFactory(Type dataType)
    {
        // Direct match (called with the interface type itself)
        if (s_dataControllers.TryGetValue(dataType, out var factory))
            return factory;

        // Walk the type's interfaces to find a registered one (called with a concrete class type)
        foreach (var iface in dataType.GetInterfaces())
            if (s_dataControllers.TryGetValue(iface, out factory))
                return factory!;

        throw new ArgumentException($"No data controller registered for '{dataType.Name}'");
    }

    /// <summary>
    /// Create an item controller for the given item data, cast to the specified constraint interface.
    /// Throws if no controller is registered for the data type or if the controller does not
    /// implement <typeparamref name="TConstraint"/>.
    /// </summary>
    public static TConstraint CreateDataController<TConstraint>(object itemData)
        where TConstraint : class
    {
        var controller = GetDataControllerFactory(itemData.GetType())();
        if (controller is not TConstraint result)
            throw new InvalidOperationException(
                $"Data controller '{controller.GetType().Name}' does not implement '{typeof(TConstraint).Name}'");
        return result;
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
        var entry = FindControllerEntry(controller, context);
        Controllable control;
        try
        {
            control = entry.Factory();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Could not create instance of '{controller}': {ex.Message}", ex);
        }

        BuildContent(control, properties, context);

        return control;
    }

    static ControlEntry FindControllerEntry(string controller, ControlCreationContext context)
    {
        // Use Panel if controller is not specified
        if (controller == "")
            return s_controllers["ZurfurGui.Controls.Panel"];

        // Check fully qualified name
        if (s_controllers.TryGetValue(controller, out var entry))
            return entry;

        // Use namespace
        if (s_controllers.TryGetValue($"{context.TypeNamespace}.{controller}", out entry))
            return entry;

        // Check uses
        foreach (var use in context.TypeUses)
            if (s_controllers.TryGetValue($"{use}.{controller}", out entry))
                return entry;

        // Check base library
        if (s_controllers.TryGetValue($"ZurfurGui.Controls.{controller}", out entry))
            return entry;

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

