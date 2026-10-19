using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZurfurGuiGen.ZuiTypes;

namespace ZurfurGuiGen;

internal static class ZuiEmitController
{
    internal static string GenerateControllerClassSource(ZuiFileInfo data)
    {
        var allBindings = data.Bindings;
        var namedControlsDict = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);

        // Add file header, usings, and namespace
        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Generate a PropertyKey for each view-facing property.
        // For generic controls, keys must live in a non-generic companion static class to avoid
        // per-closed-type duplication (each closed type would try to register the same key name,
        // causing a duplicate-registration exception in PropertyKey's constructor).
        // Data and forwarded bindings remain available through DataContext and do not need view property keys.
        var newBindings = allBindings
            .Where(b => b.BindType.HasPropertyKey())
            .ToList();
        if (data.TypeParam != "")
        {
            sb.Append($"/// <summary>Non-generic companion holding PropertyKey fields for {data.ControllerName}&lt;{data.TypeParam}&gt;.</summary>\r\n");
            sb.Append($"public static partial class {data.ControllerName}\r\n{{\r\n");
            if (newBindings.Count > 0)
            {
                sb.AppendIndentedLine(1, "// Property Keys");
                foreach (var binding in newBindings)
                {
                    ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
                    var defaultValue = GetPropertyKeyDefaultValue(binding);
                    var flagsParam = GetPropertyKeyFlags(binding);
                    var keyType = GetPropertyKeyType(binding, namedControlsDict);
                    var propertyType = GetZuiPropertyTypeName(binding);
                    var propertyKeyNameLiteral = GetPropertyKeyNameLiteral(binding, data);
                    sb.AppendIndentedLine(1,
                        $"public static readonly PropertyKey<{keyType}> {binding.PropertyKeyName}"
                            + $" = new({propertyKeyNameLiteral}, typeof({data.ControllerName}<>), {propertyType}, {defaultValue}{flagsParam});");
                }
            }
            sb.Append("}\r\n\r\n");
        }

        // Add class header
        ZuiEmit.AppendXmlDocComment(sb, 0, data.Comment);
        var partialKeyword = data.UserSuppliedControllerClass ? "partial " : "";
        if (data.TypeParam != "")
        {
            // Generic controllers use a concrete data type parameter with a handwritten constraint.
            var constraintInterface = ZuiEmit.GetConstraintType(data.TypeParamConstraint);
            sb.Append($"public sealed {partialKeyword}class {data.FileName}<{data.TypeParam}>"
                + " : global::ZurfurGui.Base.Controllable\r\n");
            sb.Append($"    where {data.TypeParam} : {constraintInterface}\r\n{{\r\n");
        }
        else
        {
            sb.Append($"public sealed {partialKeyword}class {data.FileName}"
                + " : global::ZurfurGui.Base.Controllable\r\n{\r\n");
        }

        // Add class variables
        sb.AppendIndentedLine(1, "public global::ZurfurGui.Base.View View { get; private set; } = null!; // Set by InitializeControl");
        if (allBindings.Any(b => b.BindType.HasPropertyKey()))
            sb.AppendIndentedLine(1, "public PropertiesAccessor Properties => new(View);");
        if (data.TypeParam != "")
            // For generic controls, TypeName is the open base name (e.g. "ComboBox").
            // Closed forms (e.g. "ComboBox<ComboBoxItemTextData>") are registered separately in ZurfurMain.g.cs.
            sb.AppendIndentedLine(1, $"public string TypeName => \"{data.ControllerName}\";");
        else
            sb.AppendIndentedLine(1, $"public string TypeName => \"{data.ControllerName}\";");
        sb.AppendIndentedLine(1, $"public string TypeNamespace => \"{data.Namespace}\";");
        sb.AppendIndentedLine(1, $"public TextLines TypeUses => new TextLines([{string.Join(",",
            data.Use.Select(s => "\"" + s + "\""))}]);");
        if (data.TypeParam != "" && newBindings.Count > 0)
        {
            // Static constructor: touching one companion field forces the companion's static
            // constructor to run whenever any closed form's static constructor runs.
            // ZurfurMain.g.cs calls RunClassConstructor for both ComboBox<> and each closed form,
            // so all keys are registered before style sheets are loaded.
            var firstKey = newBindings[0].PropertyKeyName;
            sb.AppendIndentedLine(1, $"// Touching {data.ControllerName}.{firstKey} ensures the companion static class");
            sb.AppendIndentedLine(1, $"// initializes (registering all PropertyKeys) when any closed form runs.");
            sb.AppendIndentedLine(1, $"static {data.FileName}() {{ _ = {data.ControllerName}.{firstKey}; }}");
            sb.Append("\r\n");
        }
        else if (newBindings.Count > 0)
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(1, "// Property Keys");
            foreach (var binding in newBindings)
            {
                var openType = data.ControllerName;
                ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
                var defaultValue = GetPropertyKeyDefaultValue(binding);
                var flagsParam = string.IsNullOrWhiteSpace(binding.Flags)
                    ? ""
                    : $", {binding.Flags}";
                var propertyType = GetZuiPropertyTypeName(binding);
                var propertyKeyNameLiteral = GetPropertyKeyNameLiteral(binding, data);
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {binding.PropertyKeyName}"
                        + $" = new({propertyKeyNameLiteral}, typeof({openType}), {propertyType}, {defaultValue}{flagsParam});");
            }
        }

        // Generate JSON property info for data and style-only properties
        GenerateZuiPropertyInfoDictionary(allBindings, sb, data);

        // Data bindings - generate full property with event hookup if there are "new" bindings       
        GenerateDataContextProperty(data, sb);
        sb.Append("\r\n");

        // Create named control variables
        Json.RemoveKeys(data.JsonDocument, new List<string> { "#comment", "$namespace", "$use", "$data" });
        var zuiJsonContent = Json.Serialize(data.JsonDocument).Replace("\"", "\"\"");
        var controlNames = namedControlsDict.Keys.OrderBy(n => n);
        sb.AppendIndentedLine(1, "// Named controls (public unless name starts with '_')");
        foreach (var name in controlNames)
        {
            var qualifier = name.StartsWith("_") ? "private" : "public ";
            sb.AppendIndentedLine(1, "");
            ZuiEmit.AppendXmlDocComment(sb, 1, namedControlsDict[name].Comment);
            sb.AppendIndentedLine(1, $"{qualifier} {namedControlsDict[name].Type} {name} = null!; // Set by InitializeControl");
        }

        // Add constructor if no .cs file is supplied
        var constructor = "";
        if (!data.UserSuppliedControllerClass)
        {
            // .cs class is not supplied (create a constructor)
            constructor = $"    // No .cs file detected, so generate constructor\r\n"
                + $"    public {data.FileName}()\r\n    {{\r\n        InitializeControl();\r\n    }}\r\n";
            sb.Append("\r\n").Append(constructor);
        }

        // Add InitializeControl header
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void InitializeControl()");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "View = new(this);");
        sb.AppendIndentedLine(2, "global::ZurfurGui.Loader.Load(this, _zuiJsonContent);");
        sb.Append("\r\n");

        // Add InitializeControl code to initialize named controls
        if (controlNames.Any())
            sb.AppendIndentedLine(2, "// Initialize named controls");
        foreach (var name in controlNames)
            sb.AppendIndentedLine(2, $"{name} = ({namedControlsDict[name].Type})View.FindByName(\"{name}\").Controller;");

        // Initialize DataContext after controls are loaded
        if (allBindings.Any(b => !b.BindType.IsViewOnly()))
        {
            sb.AppendIndentedLine(2, "// Initialize DataContext");
            sb.AppendIndentedLine(2, "DataContext = CreateDefaultDataContext();");
            sb.Append("\r\n");
        }
        sb.AppendIndentedLine(1, "}");

        // Generate CreateDefaultDataContext factory method
        var dataBindings = allBindings.Where(b => !b.BindType.IsViewOnly()).ToList();
        if (dataBindings.Count != 0)
        {
            sb.Append("\r\n");
            var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
            sb.AppendIndentedLine(1, $"{data.ControllerName}Data{genericSuffix} CreateDefaultDataContext()");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"return new {data.ControllerName}Data{genericSuffix}");
            sb.AppendIndentedLine(2, "{");
            foreach (var binding in dataBindings)
            {
                string value;
                if (ZuiEmit.IsNamedControl(binding.Bind, namedControlsDict))
                {
                    // If this binds directly to a named control, use the data context
                    value = $"{binding.Bind}.DataContext";
                }
                else if (binding.BindType == BindType.Forwarded && !binding.IsCollection && binding.Bind.Contains('.'))
                {
                    // Forwarded values default to the current value on their target control.
                    value = TransformBindingPath(binding.Bind);
                }
                else if (binding.IsCollection)
                {
                    // Collection: initialize with empty ObservableCollection
                    value = $"new {ZuiEmit.GetBindingDataType(binding, namedControlsDict)}()";
                }
                else
                {
                    // For all other types, initialize with default value or new instance
                    if (!string.IsNullOrWhiteSpace(binding.Default))
                        value = ZuiEmit.NormalizeDefaultValue(binding.Default);
                    else if (ZuiEmit.GetImplicitDefaultValue(binding) is { Length: > 0 } implicitDefault)
                        value = implicitDefault;
                    else if (binding.IsNullable)
                        value = "null";
                    else
                        value = $"new {binding.BaseType}()";
                }
                sb.AppendIndentedLine(3, $"{binding.PascalName} = {value},");
            }

            sb.AppendIndentedLine(2, "};");
            sb.AppendIndentedLine(1, "}");

            // Generate event handler for DataContext property changes (only if there are "new" bindings)
            GenerateOnDataContextPropertyChanged(allBindings, sb, data);
            GenerateSyncAllPropertiesToView(allBindings, sb, data);
            GenerateSetDataProperty(allBindings, sb, namedControlsDict, data);

        }

        if (allBindings.Any(b => b.BindType.HasPropertyKey()))
            GeneratePropertiesAccessor(allBindings, sb, data);

        // Access to JSON content
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");

        sb.Append("}");
        return sb.ToString();
    }

    private static void GeneratePropertiesAccessor(IEnumerable<DataBinding> bindings, StringBuilder sb, ZuiFileInfo data)
    {
        var keyPrefix = data.TypeParam != "" ? $"{data.ControllerName}." : "";
        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        var viewBindings = bindings.Where(b => b.BindType.HasPropertyKey());
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public struct PropertiesAccessor");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "readonly global::ZurfurGui.Base.View _view;");
        sb.AppendIndentedLine(2, "internal PropertiesAccessor(global::ZurfurGui.Base.View view) => _view = view;");

        foreach (var binding in viewBindings)
        {
            var key = $"{keyPrefix}{binding.PropertyKeyName}";
            if (binding.Name.StartsWith("$"))
                key = $"{data.ControllerName}.{binding.PropertyKeyName}";
            var propertyType = ZuiEmit.GetBindingDataType(binding, namedControls);
            sb.Append("\r\n");
            ZuiEmit.AppendXmlDocComment(sb, 2, binding.Comment);
            var readMethod = "GetProperty";
            var accessDescription = binding.BindType == BindType.StyledData
                ? "read-only; settable from the DataContext only"
                : "read/write through the view";
            sb.AppendIndentedLine(2,
                $"/// <remarks>Binding: {binding.BindType}; reads with {readMethod}; {accessDescription}.</remarks>");
            sb.AppendIndentedLine(2, $"public {propertyType} {binding.PascalName}");
            sb.AppendIndentedLine(2, "{");
            switch (binding.BindType)
            {
                case BindType.StyledData:
                case BindType.Styled:
                case BindType.AttachedStyle:
                case BindType.Property:
                case BindType.AttachedProperty:
                    sb.AppendIndentedLine(3, $"get => _view.GetProperty({key});");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported binding type '{binding.BindType}'.");
            }
            if (binding.BindType.IsViewOnly())
            {
                sb.AppendIndentedLine(3, "set");
                sb.AppendIndentedLine(3, "{");
                if (binding.IsNullable)
                {
                    sb.AppendIndentedLine(4, $"if (value is {binding.BaseType} nonNullValue)");
                    sb.AppendIndentedLine(5, $"_view.SetProperty({key}, nonNullValue);");
                    sb.AppendIndentedLine(4, "else");
                    sb.AppendIndentedLine(5, $"_view.RemoveProperty({key});");
                }
                else
                {
                    sb.AppendIndentedLine(4, $"_view.SetProperty({key}, value);");
                }
                sb.AppendIndentedLine(3, "}");
            }
            sb.AppendIndentedLine(2, "}");
        }

        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateDataContextProperty(ZuiFileInfo data, StringBuilder sb)
    {
        if (data.Bindings.Count == 0 || !data.Bindings.Any(b => !b.BindType.IsViewOnly()))
            return;

        var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
        var dataType = $"{data.ControllerName}Data{genericSuffix}";

        // Generate backing field and full property with event hookup
        sb.AppendIndentedLine(1, $"{dataType} _dataContext = null!; // Set by InitializeControl");
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, $"public {dataType} DataContext");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "get => _dataContext;");
        sb.AppendIndentedLine(2, "set");
        sb.AppendIndentedLine(2, "{");
        sb.AppendIndentedLine(3, "if (_dataContext == value) return;");
        sb.AppendIndentedLine(3, "if (_dataContext != null)");
        sb.AppendIndentedLine(4, "_dataContext.PropertyChanged -= OnDataContextPropertyChanged;");
        sb.AppendIndentedLine(3, "_dataContext = value;");
        sb.AppendIndentedLine(3, "if (_dataContext != null)");
        sb.AppendIndentedLine(3, "{");
        sb.AppendIndentedLine(4, "_dataContext.PropertyChanged += OnDataContextPropertyChanged;");
        sb.AppendIndentedLine(4, "SyncAllPropertiesToView();");
        sb.AppendIndentedLine(3, "}");
        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateOnDataContextPropertyChanged(IEnumerable<DataBinding> bindings, StringBuilder sb, ZuiFileInfo data)
    {
        var keyPrefix = data.TypeParam != "" ? $"{data.ControllerName}." : "";
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void OnDataContextPropertyChanged(object ?sender, PropertyChangedEventArgs e)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "switch (e.PropertyName)");
        sb.AppendIndentedLine(2, "{");

        // Generate a case for each binding 
        foreach (var binding in bindings)
        {
            // Collection bindings: the control subscribes to CollectionChanged itself
            if (binding.IsCollection)
            {
                sb.AppendIndentedLine(3, $"// '{binding.PascalName}': Collection binding: control manages CollectionChanged internally");
            }
            else if (binding.BindType == BindType.StyledData)
            {
                // Handle "styledData" bindings
                sb.AppendIndentedLine(3, $"case \"{binding.PascalName}\":");
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(4, $"if (DataContext.{binding.PascalName} is {binding.BaseType} nonNull{binding.PascalName})");
                    sb.AppendIndentedLine(5, $"View.SetProperty({keyPrefix}{binding.PropertyKeyName}, nonNull{binding.PascalName});");
                    sb.AppendIndentedLine(4, "else");
                    sb.AppendIndentedLine(5, $"View.RemoveProperty({keyPrefix}{binding.PropertyKeyName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(4, $"View.SetProperty({keyPrefix}{binding.PropertyKeyName}, DataContext.{binding.PascalName});");
                }
                sb.AppendIndentedLine(4, "break;");

            }
            else if (binding.BindType == BindType.Forwarded)
            {
                // Handle forwarding bindings
                sb.AppendIndentedLine(3, $"case \"{binding.PascalName}\":");
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(4, "// TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(4, $"{targetPath} = DataContext.{binding.PascalName};");
                }
                sb.AppendIndentedLine(4, "break;");
            }
            // Data-only bindings: no PropertyKey, nothing to push to the view
            else if (binding.BindType == BindType.Data)
            {
                // Check if this data-only binding has flags
                if (!string.IsNullOrWhiteSpace(binding.Flags))
                {
                    // Generate a case statement that calls View.SetFlags
                    sb.AppendIndentedLine(3, $"case \"{binding.PascalName}\":");
                    sb.AppendIndentedLine(4, $"View.SetFlags({binding.Flags});");
                    sb.AppendIndentedLine(4, "break;");
                }
                else
                {
                    // No flags: just leave a comment
                    sb.AppendIndentedLine(3, $"// '{binding.PascalName}': Data-only binding: stored in DataContext only, no view property to update");
                }
            }
            // View-only bindings: no DataContext field, controlled via styles or imperative SetProperty
            else if (binding.BindType.IsViewOnly())
            {
                sb.AppendIndentedLine(3, $"// '{binding.PascalName}': Style-only binding: no DataContext field, set via stylesheets or view.SetProperty");
            }
            else
            {
                sb.AppendIndentedLine(3, $"// '{binding.PascalName}':: BindType='{binding.BindType}', Bind='{binding.Bind}'");
            }

        }

        // Handle null or empty PropertyName (means all properties changed)
        sb.AppendIndentedLine(3, "case null:");
        sb.AppendIndentedLine(3, "case \"\":");
        sb.AppendIndentedLine(4, "SyncAllPropertiesToView();");
        sb.AppendIndentedLine(4, "break;");

        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSyncAllPropertiesToView(IEnumerable<DataBinding> bindings, StringBuilder sb, ZuiFileInfo data)
    {
        var keyPrefix = data.TypeParam != "" ? $"{data.ControllerName}." : "";
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void SyncAllPropertiesToView()");
        sb.AppendIndentedLine(1, "{");

        foreach (var binding in bindings)
        {
            // Collection bindings: the control manages CollectionChanged internally
            if (binding.IsCollection)
            {
                sb.AppendIndentedLine(2, $"// '{binding.PascalName}': TBD: Collection binding, BindType = '{binding.BindType}', Bind='{binding.Bind}'");
                continue;
            }

            // View-only bindings: no DataContext field, skip sync
            if (binding.BindType.IsViewOnly())
            {
                // No DataContext field to sync from
                continue;
            }

            // Handle "styled" bindings
            if (binding.BindType == BindType.StyledData)
            {
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(2, $"if (DataContext.{binding.PascalName} is {binding.BaseType} nonNull{binding.PascalName})");
                    sb.AppendIndentedLine(3, $"View.SetProperty({keyPrefix}{binding.PropertyKeyName}, nonNull{binding.PascalName});");
                    sb.AppendIndentedLine(2, "else");
                    sb.AppendIndentedLine(3, $"View.RemoveProperty({keyPrefix}{binding.PropertyKeyName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(2, $"View.SetProperty({keyPrefix}{binding.PropertyKeyName}, DataContext.{binding.PascalName});");
                }
            }
            else if (binding.BindType == BindType.Forwarded)
            {
                // Handle forwarding bindings
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(2, $"// '{binding.PascalName}': TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(2, $"{targetPath} = DataContext.{binding.PascalName};");
                }
            }
            else
            {
                sb.AppendIndentedLine(2, $"// '{binding.PascalName}': TBD: BindType = '{binding.BindType}', Bind='{binding.Bind}'");
            }
        }

        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSetDataProperty(IEnumerable<DataBinding> bindings, StringBuilder sb, Dictionary<string, NamedControlInfo> namedControls, ZuiFileInfo data)
    {
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public bool SetDataProperty(string name, object? value)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "switch (name)");
        sb.AppendIndentedLine(2, "{");

        foreach (var binding in bindings)
        {
            // Skip view-only properties: they have no DataContext field
            if (binding.BindType.IsViewOnly())
                continue;

            var jsonName = binding.Name; // Keep for error messages
            var dataType = ZuiEmit.GetBindingDataType(binding, namedControls);
            var baseType = binding.BaseType;

            sb.AppendIndentedLine(3, $"case \"{binding.PascalName}\":");

            if (binding.IsCollection)
            {
                // Collection binding: accept the ObservableCollection type
                sb.AppendIndentedLine(4, $"if (value is {dataType} typedValue{binding.PascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{binding.PascalName} = typedValue{binding.PascalName};");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
                sb.AppendIndentedLine(4, "return true;");
                continue;
            }

            // If binding to a named control, use its concrete generated data type for pattern matching
            var matchType = ZuiEmit.IsNamedControl(binding.Bind, namedControls) 
                ? $"{baseType}Data" 
                : baseType;

            // Use pattern matching to handle nullable/non-nullable scenarios
            if (binding.IsNullable)
            {
                // Target is nullable - accept null or the base type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{binding.PascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{binding.PascalName} = typedValue{binding.PascalName};");
                sb.AppendIndentedLine(4, $"else if (value is null)");
                sb.AppendIndentedLine(5, $"DataContext.{binding.PascalName} = null;");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
            }
            else
            {
                // Target is non-nullable - must be the correct type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{binding.PascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{binding.PascalName} = typedValue{binding.PascalName};");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
            }

            sb.AppendIndentedLine(4, "return true;");
        }

        sb.AppendIndentedLine(3, "default:");
        sb.AppendIndentedLine(4, "return false;");
        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    /// <summary>
    /// Transform a binding path like "_checkText.text" to "_checkText.DataContext.Text".
    /// Inserts "DataContext" after the first segment and PascalCases remaining segments.
    /// </summary>
    static string TransformBindingPath(string bindPath)
    {
        var parts = bindPath.Split('.');
        if (parts.Length == 1)
        {
            // Edge case: no '.' in the path
            return bindPath + ".DataContext";
        }

        var controlRef = parts[0];
        var remainingParts = parts.Skip(1).Select(ZuiEmit.ToPascalCase);
        return controlRef + ".DataContext." + string.Join(".", remainingParts);
    }

    /// <summary>
    /// Selects the key's CLR type. Generic type parameters cannot appear in the non-generic companion class, so those
    /// metadata-only keys use object. Collections and direct named-control bindings use their generated data type.
    /// </summary>
    private static string GetPropertyKeyType(DataBinding binding, Dictionary<string, NamedControlInfo> namedControls)
    {
        if (binding.IsTypeParam)
            return "object";
        if (binding.IsCollection || ZuiEmit.IsNamedControl(binding.Bind, namedControls))
            return ZuiEmit.GetBindingDataType(binding, namedControls);
        return binding.BaseType;
    }

    /// <summary>
    /// Data and forwarded keys are metadata-only; default! supplies default(T) without constructing an arbitrary data
    /// type or requiring a public parameterless constructor. View-backed keys use the declared default or new() as
    /// their style default.
    /// </summary>
    private static string GetPropertyKeyDefaultValue(DataBinding binding)
    {
        if (binding.BindType is BindType.Data or BindType.Forwarded)
            return "default!";
        if (!string.IsNullOrWhiteSpace(binding.Default))
            return ZuiEmit.NormalizeDefaultValue(binding.Default);
        var implicitDefault = ZuiEmit.GetImplicitDefaultValue(binding);
        if (implicitDefault.Length > 0)
            return implicitDefault;
        return "new()";
    }

    /// <summary>
    /// Omits the optional flags argument when no flags were declared; otherwise passes the binding's ViewFlags through
    /// to the PropertyKey so normal view invalidation behavior is preserved.
    /// </summary>
    private static string GetPropertyKeyFlags(DataBinding binding)
    {
        return string.IsNullOrWhiteSpace(binding.Flags)
            ? ""
            : $", {binding.Flags}";
    }

    private static string GetZuiPropertyTypeName(DataBinding binding)
    {
        return binding.BindType switch
        {
            BindType.Data or BindType.Forwarded => "ZuiPropertyType.Data",
            BindType.StyledData => "ZuiPropertyType.StyledData",
            BindType.Styled => "ZuiPropertyType.Styled",
            BindType.Property => "ZuiPropertyType.Property",
            BindType.AttachedProperty => "ZuiPropertyType.AttachedProperty",
            BindType.AttachedStyle => "ZuiPropertyType.AttachedStyle",
            _ => throw new InvalidOperationException($"Unsupported binding type '{binding.BindType}'.")
        };
    }

    // Preserves legacy registered names for ordinary bindings, while dollar-prefixed Panel bindings use their
    // explicit keyName override or the literal binding name. Escape the value for generated C# source because
    // keyName accepts arbitrary non-empty strings; quotes, backslashes, and control characters must remain valid.
    private static string GetPropertyKeyNameLiteral(DataBinding binding, ZuiFileInfo data)
    {
        var propertyKeyName = binding.PropertyKeyRegistrationName;
        if (string.IsNullOrEmpty(propertyKeyName))
        {
            propertyKeyName = binding.Name.StartsWith("$")
                ? binding.Name
                : $"{data.ControllerName}.{binding.Name}";
        }

        return "\"" + propertyKeyName
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t") + "\"";
    }

    private static void GenerateZuiPropertyInfoDictionary(IEnumerable<DataBinding> bindings, StringBuilder sb, ZuiFileInfo data)
    {
        sb.Append("\r\n");

        var bindingList = bindings.ToList();
        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        if (bindingList.Count == 0)
        {
            // Generate empty dictionary for controls without data bindings
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, ZuiPropertyInfo> s_zuiPropertyInfo = new();");
        }
        else
        {
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, ZuiPropertyInfo> s_zuiPropertyInfo = new()");
            sb.AppendIndentedLine(1, "{");

            for (int i = 0; i < bindingList.Count; i++)
            {
                var binding = bindingList[i];
                var comma = i < bindingList.Count - 1 ? "," : "";
                var nullableStr = binding.IsNullable ? "true" : "false";
                var typeofStr = binding.IsCollection || ZuiEmit.IsNamedControl(binding.Bind, namedControls)
                    ? $"typeof({ZuiEmit.GetBindingDataType(binding, namedControls)})"
                    : $"typeof({binding.BaseType})";
                var propertyTypeStr = GetZuiPropertyTypeName(binding);
                var keyPrefix = data.TypeParam != "" ? $"{data.ControllerName}." : "";
                var propertyKeyIdStr = binding.BindType.HasPropertyKey()
                    ? $"{keyPrefix}{binding.PropertyKeyName}.Id"
                    : "null";
                sb.AppendIndentedLine(2, $"[\"{binding.PascalName}\"] = new(\"{binding.PascalName}\", {typeofStr}, {nullableStr}, {propertyTypeStr}, {propertyKeyIdStr}){comma}");
            }

            sb.AppendIndentedLine(1, "};");
        }

        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public IReadOnlyDictionary<string, ZuiPropertyInfo> ZuiPropertyInfo => s_zuiPropertyInfo;");
    }
}
