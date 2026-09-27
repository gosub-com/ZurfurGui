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

        // Add file header, usings, and namespace
        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Generated PropertyKey fields for "$data" entries that bind to "styledData" or "styleOnly" (excluding collections).
        // For generic controls, keys must live in a non-generic companion static class to avoid
        // per-closed-type duplication (each closed type would try to register the same key name,
        // causing a duplicate-registration exception in PropertyKey's constructor).
        var newBindings = allBindings.Where(b => (b.BindType == BindType.StyledData || b.BindType == BindType.StyledOnly) && !b.IsCollection).ToList();
        if (data.TypeParam != "" && newBindings.Count > 0)
        {
            sb.Append($"/// <summary>Non-generic companion holding PropertyKey fields for {data.ControllerName}&lt;{data.TypeParam}&gt;.</summary>\r\n");
            sb.Append($"public static partial class {data.ControllerName}\r\n{{\r\n");
            sb.AppendIndentedLine(1, "// Property Keys");
            foreach (var binding in newBindings)
            {
                ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
                var defaultValue = string.IsNullOrWhiteSpace(binding.Default)
                    ? "new()"
                    : ZuiEmit.NormalizeDefaultValue(binding.Default);
                var flagsParam = string.IsNullOrWhiteSpace(binding.Flags) || binding.Flags == "ViewFlags.None"
                    ? ""
                    : $", {binding.Flags}";
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {binding.PropertyKeyName}"
                        + $" = new(\"{data.ControllerName}.{binding.Name}\", typeof({data.ControllerName}<>), {defaultValue}{flagsParam});");
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
                var defaultValue = string.IsNullOrWhiteSpace(binding.Default)
                    ? "new()"
                    : ZuiEmit.NormalizeDefaultValue(binding.Default);
                var flagsParam = string.IsNullOrWhiteSpace(binding.Flags) || binding.Flags == "ViewFlags.None"
                    ? ""
                    : $", {binding.Flags}";
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {binding.PropertyKeyName}"
                        + $" = new(\"{data.ControllerName}.{binding.Name}\", typeof({openType}), {defaultValue}{flagsParam});");
            }
        }

        // Generate data property info dictionary (excluding styleOnly properties)
        GenerateDataPropertyInfoDictionary(allBindings.Where(b => b.BindType != BindType.StyledOnly), sb);

        // Data bindings - generate full property with event hookup if there are "new" bindings       
        GenerateDataContextProperty(data, sb);
        sb.Append("\r\n");

        // Create named control variables
        var namedControlsDict = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
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
        if (allBindings.Any(b => b.BindType != BindType.StyledOnly))
        {
            sb.AppendIndentedLine(2, "// Initialize DataContext");
            sb.AppendIndentedLine(2, "DataContext = CreateDefaultDataContext();");
            sb.Append("\r\n");
        }
        sb.AppendIndentedLine(1, "}");

        // Generate CreateDefaultDataContext factory method
        var dataBindings = allBindings.Where(b => b.BindType != BindType.StyledOnly).ToList();
        if (dataBindings.Count != 0)
        {
            sb.Append("\r\n");
            var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
            sb.AppendIndentedLine(1, $"{data.ControllerName}Data{genericSuffix} CreateDefaultDataContext()");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"return new {data.ControllerName}Data{genericSuffix}(");

            var args = new List<string>();
            foreach (var binding in dataBindings)
            {
                if (ZuiEmit.IsNamedControl(binding.Bind, namedControlsDict))
                {
                    // If this binds directly to a named control, use the data context
                    args.Add($"{binding.Name}: {binding.Bind}.DataContext");
                }
                else if (binding.IsCollection)
                {
                    // Collection: initialize with empty ObservableCollection
                    args.Add($"{binding.Name}: new {ZuiEmit.GetBindingDataType(binding, namedControlsDict)}()");
                }
                else
                {
                    // For all other types, initialize with default value or new instance
                    if (!string.IsNullOrWhiteSpace(binding.Default))
                        args.Add($"{binding.Name}: {ZuiEmit.NormalizeDefaultValue(binding.Default)}");
                    else if (binding.IsNullable)
                        args.Add($"{binding.Name}: null");
                    else
                        args.Add($"{binding.Name}: new {binding.BaseType}()");
                }
            }

            for (int i = 0; i < args.Count; i++)
            {
                var suffix = i < args.Count - 1 ? "," : "";
                sb.AppendIndentedLine(3, args[i] + suffix);
            }

            sb.AppendIndentedLine(2, ");");
            sb.AppendIndentedLine(1, "}");

            // Generate event handler for DataContext property changes (only if there are "new" bindings)
            GenerateOnDataContextPropertyChanged(allBindings, sb, data);
            GenerateSyncAllPropertiesToView(allBindings, sb, data);
            GenerateSetDataProperty(allBindings, sb, namedControlsDict, data);

        }

        // Access to JSON content
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, $"static string _zuiJsonContent => @\"{zuiJsonContent}\";");

        sb.Append("}");
        return sb.ToString();
    }

    private static void GenerateDataContextProperty(ZuiFileInfo data, StringBuilder sb)
    {
        if (data.Bindings.Count == 0 || !data.Bindings.Any(b => b.BindType != BindType.StyledOnly))
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
                if (!string.IsNullOrWhiteSpace(binding.Flags) && binding.Flags != "ViewFlags.None")
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
            // Style-only bindings: no DataContext field, controlled via styles or imperative SetProperty
            else if (binding.BindType == BindType.StyledOnly)
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

            // Style-only bindings: no DataContext field, skip sync
            if (binding.BindType == BindType.StyledOnly)
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
            // Skip style-only properties: they have no DataContext field
            if (binding.BindType == BindType.StyledOnly)
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

    private static void GenerateDataPropertyInfoDictionary(IEnumerable<DataBinding> bindings, StringBuilder sb)
    {
        sb.Append("\r\n");

        var bindingList = bindings.ToList();
        if (bindingList.Count == 0)
        {
            // Generate empty dictionary for controls without data bindings
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, DataPropertyInfo> s_dataPropertyInfo = new();");
        }
        else
        {
            sb.AppendIndentedLine(1, "static readonly Dictionary<string, DataPropertyInfo> s_dataPropertyInfo = new()");
            sb.AppendIndentedLine(1, "{");

            for (int i = 0; i < bindingList.Count; i++)
            {
                var binding = bindingList[i];
                var comma = i < bindingList.Count - 1 ? "," : "";
                var nullableStr = binding.IsNullable ? "true" : "false";
                var typeofStr = binding.IsCollection
                    ? $"typeof({ZuiEmit.GetBindingDataType(binding, new Dictionary<string, NamedControlInfo>())})"
                    : $"typeof({binding.BaseType})";
                sb.AppendIndentedLine(2, $"[\"{binding.PascalName}\"] = new(\"{binding.PascalName}\", {typeofStr}, {nullableStr}){comma}");
            }

            sb.AppendIndentedLine(1, "};");
        }

        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public IReadOnlyDictionary<string, DataPropertyInfo> DataPropertyInfo => s_dataPropertyInfo;");
    }
}
