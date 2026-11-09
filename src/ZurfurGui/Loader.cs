using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Generated;
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
internal partial class ZurfurJsonContext : JsonSerializerContext { }


/// <summary>
/// Load and build controls from JSON.
///
/// Only the JSON serialization members are public, because applications need them to serialize
/// their own data.  Everything else is internal.  Generated code in other assemblies reaches the
/// loader through the public, generated-code-only <see cref="ZuiRuntime"/>, and the platform hosts
/// reach <see cref="Init"/> through InternalsVisibleTo.
/// </summary>
public static class Loader
{
    record struct ControlEntry(Type Type, Func<ZuiControllable> Factory);

    static Dictionary<string, ControlEntry> s_controllers = new();
    static Dictionary<string, Func<Layoutable?>> s_layouts = new();

    // Maps concrete generated data type ? item controller factory.
    static Dictionary<Type, Func<object, ZuiControllable>> s_dataControllers = new();

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

    /// <summary>
    /// Gets the shared JSON serializer options configured with ZurfurGui converters and type metadata.
    /// </summary>
    /// <remarks>
    /// Register application type-info resolvers with <see cref="RegisterJsonTypeInfoResolver"/>
    /// before accessing this property. Applications can use these options to serialize their own data.
    /// </remarks>
    public static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions ??= CreateJsonSerializerOptions();

    /// <summary>
    /// Initialize the library with the built in controls, etc.
    /// </summary>
    internal static AppWindow Init(Action<AppWindow> mainAppEntry)
    {
        RuntimeHelpers.RunClassConstructor(typeof(LayoutRow).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(LayoutDock).TypeHandle);

        RegisterLayout("Panel", () => null);
        RegisterLayout("Dock", () => new LayoutDock());
        RegisterLayout("Row", () => new LayoutRow());
        RegisterLayout("Column", () => new LayoutColumn());

        ZurfurMain.MainApp();

        var appWindow = new AppWindow();
        mainAppEntry(appWindow);
        return appWindow;
    }


    /// <summary>
    /// Load a JSON file into the target object. This is the function that gets
    /// called from the InitializeControl function in the generated code.
    /// </summary>
    internal static void Load(ZuiControllable target, string json)
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

            BuildTemplateContent(target, properties, ControlCreationContext.From(target));
            ApplyDataProperties(target);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading control '{target.TypeName}': {ex.Message}, type={ex.GetType()}");
            throw;
        }
    }

    private static void BuildTemplateContent(
        ZuiControllable control,
        Properties properties,
        ControlCreationContext context)
    {
        // The properties become overrides, and the control's own content is added to its main view.
        var content = properties.Get(Panel.Content);
        properties.Remove(Panel.Content);
        control.View.MergeOverwrite(properties);
        SetLayout(properties.Get(Panel.Layout) ?? "", control.View);
        AddChildren(control.View, content, context);
    }

    private static void BuildParentContent(
        ZuiControllable control,
        Properties properties,
        ControlCreationContext context)
    {
        var content = properties.Get(Panel.Content);
        var layout = properties.Get(Panel.Layout) ?? "";

        properties.Remove(Panel.Content);
        properties.Remove(Panel.Layout);

        // Instance properties describe the control's outer view, including its size, alignment, and appearance.
        control.View.MergeOverwrite(properties);
        
        // The layout arranges parent-supplied content; applying it to the root could replace a control's template layout.
        SetLayout(layout, control.ContentHost);

        // Content supplied by the parent goes to the control's content host (the main view by default)
        AddChildren(control.ContentHost, content, context);
    }

    // Creates a control for each property set and adds its view to the host
    private static void AddChildren(View host, Properties[]? contents, ControlCreationContext context)
    {
        if (contents != null)
            foreach (var property in contents)
                host.AddChild(CreateControl(property, context).View);
    }

    /// <summary>
    /// Apply JSON properties declared in the control's $data section, recursively processing
    /// children first, then applying parent properties. Data bindings are applied through
    /// SetDataProperty; style-only bindings are applied to the view.
    /// Converts JSON camelCase names to PascalCase for the generated control API.
    /// Children are processed first so parent bindings can override child defaults.
    /// Should be called after DataContext is initialized.
    /// </summary>
    private static void ApplyDataProperties(ZuiControllable control)
    {
        // Recursively apply to all child controls
        foreach (var childView in control.View.Children)
            if (childView.Controller is ZuiControllable childControl)
                ApplyDataProperties(childControl);

        // Apply JSON properties to this control (after children)
        var dataProperties = control.View._properties.Get(Panel.DataProperties);

        // TBD: We should be able to remove these after applying, but we can't because it changes "stuff".
        //control.View._properties.Remove(Panel.DataProperties);

        if (dataProperties != null && dataProperties.Count > 0)
        {
            var propertyInfoMap = control.ZuiPropertyInfo;

            foreach (var (jsonPropertyName, jsonElement) in dataProperties)
            {
                // Convert JSON camelCase to PascalCase for lookup and API
                var pascalCaseName = ToPascalCase(jsonPropertyName);

                // Validate property exists in ZuiPropertyInfo (uses PascalCase keys)
                if (!propertyInfoMap.TryGetValue(pascalCaseName, out var propInfo))
                {
                    throw new InvalidOperationException(
                        $"JSON property '{jsonPropertyName}' is not declared in control '{control.TypeName}'. " +
                        $"Available properties: {string.Join(", ", propertyInfoMap.Keys)}");
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
                        $"Failed to deserialize JSON property '{jsonPropertyName}' to type '{propInfo.BaseType.Name}' " +
                        $"in control '{control.TypeName}': {ex.Message}", ex);
                }

                // Validate nullability
                if (value == null && !propInfo.IsNullable)
                {
                    throw new InvalidOperationException(
                        $"JSON property '{jsonPropertyName}' cannot be null (type: {propInfo.BaseType.Name}) " +
                        $"in control '{control.TypeName}'");
                }

                switch (propInfo.PropertyType)
                {
                    case ZuiPropertyType.Styled:
                    case ZuiPropertyType.Property:
                    case ZuiPropertyType.AttachedStyle:
                    case ZuiPropertyType.AttachedProperty:
                        var propertyKey = propInfo.PropertyKeyId
                            ?? throw new InvalidOperationException(
                                $"View property '{jsonPropertyName}' on control '{control.TypeName}' has no PropertyKeyId.");
                        if (value is null)
                            control.View.RemovePropertyById(propertyKey);
                        else
                            control.View.SetPropertyById(propertyKey, value);
                        break;
                    case ZuiPropertyType.Data:
                    case ZuiPropertyType.StyledData:
                        if (!control.SetDataProperty(pascalCaseName, value))
                        {
                            throw new InvalidOperationException(
                                $"Failed to set JSON property '{jsonPropertyName}' (as '{pascalCaseName}') on control '{control.TypeName}'.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported ZUI property type '{propInfo.PropertyType}' for property '{jsonPropertyName}' "
                            + $"in control '{control.TypeName}'.");
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
    /// Register a control by name with its type and factory.
    /// The factory is used by CreateControl.
    /// </summary>
    internal static void RegisterControl(string name, Type type, Func<ZuiControllable> factory)
    {
        if (s_controllers.TryGetValue(name, out var existing))
        {
            if (existing.Type == type)
                return; // Already registered
            throw new ArgumentException($"Control '{name}' is already registered for type '{existing.Type.Name}'");
        }
        if (!typeof(ZuiControllable).IsAssignableFrom(type))
            throw new ArgumentException($"Type '{type.Name}' does not implement the ZuiControllable interface.");

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
    internal static void RegisterDataController(Type dataType, Func<object, ZuiControllable> factory)
    {
        if (s_dataControllers.ContainsKey(dataType))
            throw new ArgumentException($"Data controller for '{dataType.Name}' is already registered");
        s_dataControllers[dataType] = factory;
    }

    /// <summary>
    /// Get the factory for the item controller that handles the given concrete item data type.
    /// </summary>
    internal static Func<object, ZuiControllable> GetDataControllerFactory(Type dataType)
    {
        if (s_dataControllers.TryGetValue(dataType, out var factory))
            return factory;

        throw new ArgumentException($"No data controller registered for '{dataType.Name}'");
    }

    /// <summary>
    /// Creates the controller registered for a concrete data-item instance.
    /// This method is intended to be called by controls such as <c>ComboBox&lt;Item&gt;</c> when they need to render
    /// an item. The loader uses the runtime type of <paramref name="itemData"/> to find the factory previously
    /// registered by <see cref="RegisterDataController(Type, Func{object, ZuiControllable})"/>, passes the same data
    /// item to that factory, and returns the resulting controller.
    /// </summary>
    /// <param name="itemData">
    /// The concrete data object to render. Its runtime type selects the registered controller factory, and its
    /// values are preserved when the factory assigns it to the controller's <c>DataContext</c>.
    /// </param>
    internal static ZuiControllable CreateDataController(object itemData)
    {
        return GetDataControllerFactory(itemData.GetType())(itemData);
    }

    internal static void RegisterLayout(string name, Func<Layoutable?> layoutFactory)
    {
        if (s_layouts.ContainsKey(name))
            throw new ArgumentException($"Layout '{name}' is already registered");
        s_layouts[name] = layoutFactory;
    }

    /// <summary>
    /// Create a control from the given properties with a parent/type context.
    /// (Context is not yet used for controller resolution; it is threaded through for future use.)
    /// </summary>
    internal static ZuiControllable CreateControl(Properties properties, ControlCreationContext context)
    {
        // Create the control
        var controller = properties.Get(Panel.Controller) ?? "";
        var entry = FindControllerEntry(controller, context);
        ZuiControllable control;
        try
        {
            control = entry.Factory();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Could not create instance of '{controller}': {ex.Message}", ex);
        }

        BuildParentContent(control, properties, context);

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

    private static void SetLayout(string layout, View view)
    {
        if (layout != "")
        {
            if (s_layouts.TryGetValue(layout, out var createFunc))
                view.Layout = createFunc();
            else
                throw new ArgumentException($"The layout '{layout}' is not supported");
        }
    }
}

