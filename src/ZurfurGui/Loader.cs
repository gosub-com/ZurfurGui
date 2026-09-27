using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
[JsonSerializable(typeof(ThemeSheet))]
[JsonSerializable(typeof(Dictionary<string,JsonElement>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(DockEnum))]
[JsonSerializable(typeof(Color))]
[JsonSerializable(typeof(Orientation))]
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

    // Maps concrete generated data type → item controller factory.
    static Dictionary<Type, Func<object, Controllable>> s_dataControllers = new();

    static readonly List<IJsonTypeInfoResolver> s_jsonTypeInfoResolvers = new();
    static JsonSerializerOptions? s_jsonSerializerOptions;

    /// <summary>
    /// Registers source-generated JSON metadata from the consuming application.
    /// This must be called before the first JSON load.
    /// </summary>
    public static void RegisterJsonTypeInfoResolver(IJsonTypeInfoResolver resolver)
    {
        if (s_jsonSerializerOptions != null)
            throw new InvalidOperationException("JSON type info resolvers must be registered before JSON loading starts.");

        s_jsonTypeInfoResolvers.Add(resolver);
    }

    static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var resolvers = new List<IJsonTypeInfoResolver> { ZurfurJsonContext.Default };
        resolvers.AddRange(s_jsonTypeInfoResolvers);
        resolvers.Add(new DefaultJsonTypeInfoResolver());

        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(resolvers.ToArray()),
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
    }

    public static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions ??= CreateJsonSerializerOptions();

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
        RegisterLayout("Text", () => new LayoutText());

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
        RuntimeHelpers.RunClassConstructor(typeof(Panel).TypeHandle);

        try
        {
            var properties = JsonSerializer.Deserialize<Properties>(json, s_jsonSerializerOptions)
                ?? throw new Exception($"The target control '{target.TypeName}' has invalid or null JSON");

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
            ApplyDataProperties(target);
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
        control.View.MergeOverwrite(properties);
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
    private static void ApplyDataProperties(Controllable control)
    {
        // Recursively apply to all child controls
        foreach (var childView in control.View.Children)
            ApplyDataProperties(childView.Controller);

        // Apply data properties to this control (after children)
        var dataProperties = control.View._properties.Get(Panel.DataProperties);

        // TBD: We should be able to remove these after applying, but we can't because it changes "stuff".
        //control.View._properties.Remove(Panel.DataProperties);

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
                        $"in control '{control.TypeName}': {ex.Message}", ex);
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
    /// Registers the factory used to create a controller for a concrete data-item type.
    /// This method is intended to be called by generated <c>ZurfurMain.InitializeControls</c> code during startup.
    /// The <paramref name="dataType"/> is the concrete runtime type of a data item, and the
    /// <paramref name="factory"/> maps an instance of that type to its controller.
    /// The factory's lambda parameter is the data item instance, received as <see cref="object"/> because the
    /// loader stores factories for different data types in one registry. The lambda returns the controller that
    /// renders the item and should assign that same data item to the controller's <c>DataContext</c>.
    /// </summary>
    /// <param name="dataType">The concrete data-item type used as the registry key.</param>
    /// <param name="factory">
    /// A mapping from the data item instance to its controller. The lambda receives the data item as
    /// <see cref="object"/> and returns the corresponding <see cref="Controllable"/>.
    /// </param>
    public static void RegisterDataController(Type dataType, Func<object, Controllable> factory)
    {
        if (s_dataControllers.ContainsKey(dataType))
            throw new ArgumentException($"Data controller for '{dataType.Name}' is already registered");
        s_dataControllers[dataType] = factory;
    }

    /// <summary>
    /// Get the factory for the item controller that handles the given concrete item data type.
    /// </summary>
    public static Func<object, Controllable> GetDataControllerFactory(Type dataType)
    {
        if (s_dataControllers.TryGetValue(dataType, out var factory))
            return factory;

        throw new ArgumentException($"No data controller registered for '{dataType.Name}'");
    }

    /// <summary>
    /// Creates the controller registered for a concrete data-item instance.
    /// This method is intended to be called by controls such as <c>ComboBox&lt;Item&gt;</c> when they need to render
    /// an item. The loader uses the runtime type of <paramref name="itemData"/> to find the factory previously
    /// registered by <see cref="RegisterDataController(Type, Func{object, Controllable})"/>, passes the same data
    /// item to that factory, and returns the resulting controller.
    /// </summary>
    /// <param name="itemData">
    /// The concrete data object to render. Its runtime type selects the registered controller factory, and its
    /// values are preserved when the factory assigns it to the controller's <c>DataContext</c>.
    /// </param>
    /// <returns>A controller configured to render <paramref name="itemData"/>.</returns>
    public static Controllable CreateDataController(object itemData)
    {
        return GetDataControllerFactory(itemData.GetType())(itemData);
    }

    public static void RegisterLayout(string name, Func<Layoutable?> layoutFactory)
    {
        if (s_layouts.ContainsKey(name))
            throw new ArgumentException($"Layout '{name}' is already registered");
        s_layouts[name] = layoutFactory;
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

