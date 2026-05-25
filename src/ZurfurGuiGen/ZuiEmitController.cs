using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitController
{
    internal static string GenerateControllerClassSource(ZuiTypes.FileInfo data, List<ZuiTypes.DataBinding>? inheritedBindings, string? implementsNamespace)
    {
        // Serialize JSON and escape double quotes for verbatim string
        var zuiJsonContent = Json.Serialize(data.JsonDocument).Replace("\"", "\"\"");

        // All bindings: inherited ones first, then the control's own bindings.
        var allBindings = inheritedBindings != null
            ? inheritedBindings.Concat(data.Bindings).ToList()
            : data.Bindings;

        // Add file header, usings, and namespace
        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        // Add class header
        ZuiEmit.AppendXmlDocComment(sb, 0, data.Comment);
        var partialKeyword = data.UserSuppliedControllerClass ? "partial " : "";
        var implementsInterface = data.Implements != "" ? $", I{data.Implements}" : "";
        if (data.TypeParam != "")
        {
            // Generic controller: the type parameter is the item data interface type.
            // The constraint keeps the data layer concrete while the controller stays generic.
            var constraintInterface = $"I{data.TypeParamConstraint}Data";
            sb.Append($"public sealed {partialKeyword}class {data.FileName}<{data.TypeParam}>"
                + $" : global::ZurfurGui.Base.Controllable{implementsInterface}\r\n");
            sb.Append($"    where {data.TypeParam} : {constraintInterface}\r\n{{\r\n");
        }
        else
        {
            sb.Append($"public sealed {partialKeyword}class {data.FileName}"
                + $" : global::ZurfurGui.Base.Controllable{implementsInterface}\r\n{{\r\n");
        }

        // Add class variables
        sb.AppendIndentedLine(1, "public global::ZurfurGui.Base.View View { get; private set; } = null!; // Set by InitializeControl");
        if (data.TypeParam != "")
            // For generic controls, TypeName is the open base name (e.g. "ComboBox").
            // Closed forms (e.g. "ComboBox<IComboBoxItemTextData>") are registered separately in ZurfurMain.g.cs.
            sb.AppendIndentedLine(1, $"public string TypeName => \"{data.ControllerName}\";");
        else
            sb.AppendIndentedLine(1, $"public string TypeName => \"{data.ControllerName}\";");
        sb.AppendIndentedLine(1, $"public string TypeNamespace => \"{data.Namespace}\";");
        sb.AppendIndentedLine(1, $"public TextLines TypeUses => new TextLines([{string.Join(",",
            data.Use.Select(s => "\"" + s + "\""))}]);");
        // Register the controller's own concrete data interface (e.g. IComboBoxItemBadgeData),
        // NOT the base .implements interface (e.g. IComboBoxItemData).
        // GetDataControllerFactory walks a concrete data class's interfaces to find a registered
        // factory; if two controllers both register IComboBoxItemData, whichever registered first
        // wins and the wrong controller is instantiated for the other item type.
        var implementsArray = data.Implements != ""
            ? $"new global::System.Type[] {{ typeof(global::{data.Namespace}.I{data.ControllerName}Data) }}"
            : "global::System.Array.Empty<global::System.Type>()";
        sb.AppendIndentedLine(1, $"public static readonly global::System.Type[] s_implementsDataInterfaces = {implementsArray};");
        sb.AppendIndentedLine(1, "public global::System.Type[] ImplementsDataInterfaces => s_implementsDataInterfaces;");

        // Generated PropertyKey fields for ".data" entries that bind to "new" (excluding collections).
        // For generic controls, keys must live in a non-generic companion static class to avoid
        // per-closed-type duplication (each closed type would try to register the same key name,
        // causing a duplicate-registration exception in PropertyKey's constructor).
        var newBindings = allBindings.Where(b => b.Bind == "styled" && !b.IsCollection).ToList();
        if (data.TypeParam != "" && newBindings.Count > 0)
        {
            // Close the generic class temporarily, emit the companion static class, then reopen.
            sb.Append("}\r\n\r\n");
            sb.Append($"/// <summary>Non-generic companion holding PropertyKey fields for {data.ControllerName}&lt;{data.TypeParam}&gt;.</summary>\r\n");
            sb.Append($"public static partial class {data.ControllerName}\r\n{{\r\n");
            sb.AppendIndentedLine(1, "// Property Keys");
            foreach (var binding in newBindings)
            {
                var propertyName = ZuiEmit.ToPascalCase(binding.Name);
                ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {propertyName}"
                        + $" = new(\"{data.ControllerName}.{binding.Name}\", typeof({data.ControllerName}<>), new());");
            }
            sb.Append("}\r\n\r\n");
            // Reopen the generic class
            var partialKeyword2 = data.UserSuppliedControllerClass ? "partial " : "";
            var constraintInterface2 = $"I{data.TypeParamConstraint}Data";
            var implementsInterface2 = data.Implements != "" ? $", I{data.Implements}" : "";
            sb.Append($"public sealed {partialKeyword2}class {data.FileName}<{data.TypeParam}>"
                + $" : global::ZurfurGui.Base.Controllable{implementsInterface2}\r\n");
            sb.Append($"    where {data.TypeParam} : {constraintInterface2}\r\n{{\r\n");

            // Static constructor: touching one companion field forces the companion's static
            // constructor to run whenever any closed form's static constructor runs.
            // ZurfurMain.g.cs calls RunClassConstructor for both ComboBox<> and each closed form,
            // so all keys are registered before style sheets are loaded.
            var firstKey = ZuiEmit.ToPascalCase(newBindings[0].Name);
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
                var propertyName = ZuiEmit.ToPascalCase(binding.Name);
                var openType = data.ControllerName;
                ZuiEmit.AppendXmlDocComment(sb, 1, binding.Comment);
                sb.AppendIndentedLine(1,
                    $"public static readonly PropertyKey<{binding.BaseType}> {propertyName}"
                        + $" = new(\"{data.ControllerName}.{binding.Name}\", typeof({openType}), new());");
            }
        }

        // Generate data property info dictionary
        GenerateDataPropertyInfoDictionary(allBindings, sb);

        // Data bindings - generate full property with event hookup if there are "new" bindings       
        GenerateDataContextProperty(data, sb);
        sb.Append("\r\n");

        // Create named control variables
        var namedControlsDict = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);
        var controlNames = namedControlsDict.Keys.OrderBy(n => n);
        sb.AppendIndentedLine(1, "// Named controls (public unless name starts with '_')");
        foreach (var name in controlNames)
        {
            var qualifier = name.StartsWith("_") ? "private" : "public ";
            sb.AppendIndentedLine(1, $"{qualifier} {namedControlsDict[name]} {name} = null!; // Set by InitializeControl");
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
        sb.AppendIndentedLine(2, "// Initialize named controls");
        foreach (var name in controlNames)
            sb.AppendIndentedLine(2, $"{name} = ({namedControlsDict[name]})View.FindByName(\"{name}\").Controller;");

        // Initialize DataContext after controls are loaded
        if (allBindings.Count != 0)
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(2, "// Initialize DataContext");
            sb.AppendIndentedLine(2, "DataContext = CreateDefaultDataContext();");
        }

        // Always apply data properties recursively (for self and all children)
        sb.Append("\r\n");
        sb.AppendIndentedLine(2, "// Apply data properties from JSON (recursively)");
        sb.AppendIndentedLine(2, "global::ZurfurGui.Loader.ApplyDataProperties(this);");

        sb.AppendIndentedLine(1, "}");

        // Generate CreateDefaultDataContext factory method
        if (allBindings.Count != 0)
        {
            sb.Append("\r\n");
            var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
            sb.AppendIndentedLine(1, $"I{data.ControllerName}Data{genericSuffix} CreateDefaultDataContext()");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"return new {data.ControllerName}Data{genericSuffix}(");

            var args = new List<string>();
            foreach (var binding in allBindings)
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
                    // For all other types, initialize with new instance
                    if (binding.IsNullable)
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

        // Explicit interface implementation for .implements contract.
        // The public DataContext property is typed as I{ControllerName}Data (e.g. IComboBoxItemTextData),
        // but I{Implements} (e.g. IComboBoxItem) requires DataContext typed as I{Implements}Data.
        // C# won't satisfy the interface via the public property since types differ even when one
        // implements the other, so this explicit implementation bridges the two with casts.
        if (data.Implements != "")
        {
            sb.Append("\r\n");
            sb.AppendIndentedLine(1, $"// Explicit interface implementation for I{data.Implements}.");
            sb.AppendIndentedLine(1, $"// The public DataContext is typed as I{data.ControllerName}Data, but I{data.Implements} requires");
            sb.AppendIndentedLine(1, $"// I{data.Implements}Data. C# won't satisfy the interface via the public property even when one");
            sb.AppendIndentedLine(1, $"// type implements the other, so this explicit implementation bridges the two with casts.");
            sb.AppendIndentedLine(1, $"I{data.Implements}Data I{data.Implements}.DataContext");
            sb.AppendIndentedLine(1, "{");
            sb.AppendIndentedLine(2, $"get => (I{data.Implements}Data)DataContext;");
            sb.AppendIndentedLine(2, $"set => DataContext = (I{data.ControllerName}Data)value;");
            sb.AppendIndentedLine(1, "}");
        }

        sb.Append("}");
        return sb.ToString();
    }

    private static void GenerateDataContextProperty(ZuiTypes.FileInfo data, StringBuilder sb)
    {
        if (data.Bindings.Count == 0)
            return;

        var genericSuffix = data.TypeParam != "" ? $"<{data.TypeParam}>" : "";
        var dataType = $"I{data.ControllerName}Data{genericSuffix}";

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

    private static void GenerateOnDataContextPropertyChanged(IEnumerable<ZuiTypes.DataBinding> bindings, StringBuilder sb, ZuiTypes.FileInfo data)
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
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);
            sb.AppendIndentedLine(3, $"case \"{pascalName}\":");

            // Collection bindings: the control subscribes to CollectionChanged itself
            if (binding.IsCollection)
            {
                sb.AppendIndentedLine(4, "// Collection binding: control manages CollectionChanged internally");
            }
            // Data-only bindings: no PropertyKey, nothing to push to the view
            else if (binding.Bind == "data")
            {
                sb.AppendIndentedLine(4, "// Data-only binding: stored in DataContext only, no view property to update");
            }
            // Handle "styled" bindings
            else if (binding.Bind == "styled")
            {
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(4, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                    sb.AppendIndentedLine(5, $"View.SetProperty({keyPrefix}{pascalName}, nonNull{pascalName});");
                    sb.AppendIndentedLine(4, "else");
                    sb.AppendIndentedLine(5, $"View.RemoveProperty({keyPrefix}{pascalName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(4, $"View.SetProperty({keyPrefix}{pascalName}, DataContext.{pascalName});");
                }
            }
            else
            {
                // Handle forwarding bindings
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(4, "// TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(4, $"{targetPath} = DataContext.{pascalName};");
                }
            }

            sb.AppendIndentedLine(4, "break;");
        }

        // Handle null or empty PropertyName (means all properties changed)
        sb.AppendIndentedLine(3, "case null:");
        sb.AppendIndentedLine(3, "case \"\":");
        sb.AppendIndentedLine(4, "SyncAllPropertiesToView();");
        sb.AppendIndentedLine(4, "break;");

        sb.AppendIndentedLine(2, "}");
        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSyncAllPropertiesToView(IEnumerable<ZuiTypes.DataBinding> bindings, StringBuilder sb, ZuiTypes.FileInfo data)
    {
        var keyPrefix = data.TypeParam != "" ? $"{data.ControllerName}." : "";
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "void SyncAllPropertiesToView()");
        sb.AppendIndentedLine(1, "{");

        foreach (var binding in bindings)
        {
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);

            // Collection bindings: the control manages CollectionChanged internally
            if (binding.IsCollection)
                continue;

            // Data-only bindings: no PropertyKey, nothing to push to the view
            if (binding.Bind == "data")
                continue;

            // Handle "styled" bindings
            if (binding.Bind == "styled")
            {
                if (binding.IsNullable)
                {
                    // Handle nullable types: SetProperty or RemoveProperty
                    sb.AppendIndentedLine(2, $"if (DataContext.{pascalName} is {binding.BaseType} nonNull{pascalName})");
                    sb.AppendIndentedLine(3, $"View.SetProperty({keyPrefix}{pascalName}, nonNull{pascalName});");
                    sb.AppendIndentedLine(2, "else");
                    sb.AppendIndentedLine(3, $"View.RemoveProperty({keyPrefix}{pascalName});");
                }
                else
                {
                    // Handle non-nullable types: SetProperty
                    sb.AppendIndentedLine(2, $"View.SetProperty({keyPrefix}{pascalName}, DataContext.{pascalName});");
                }
            }
            else
            {
                // Handle forwarding bindings
                if (!binding.Bind.Contains('.'))
                {
                    // Edge case: no '.' in binding path
                    sb.AppendIndentedLine(2, "// TBD: Resolve DataContext binding");
                }
                else
                {
                    var targetPath = TransformBindingPath(binding.Bind);
                    sb.AppendIndentedLine(2, $"{targetPath} = DataContext.{pascalName};");
                }
            }
        }

        sb.AppendIndentedLine(1, "}");
    }

    private static void GenerateSetDataProperty(IEnumerable<ZuiTypes.DataBinding> bindings, StringBuilder sb, Dictionary<string, string> namedControls, ZuiTypes.FileInfo data)
    {
        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public bool SetDataProperty(string name, object? value)");
        sb.AppendIndentedLine(1, "{");
        sb.AppendIndentedLine(2, "switch (name)");
        sb.AppendIndentedLine(2, "{");

        foreach (var binding in bindings)
        {
            var jsonName = binding.Name; // Keep for error messages
            var pascalName = ZuiEmit.ToPascalCase(binding.Name);
            var dataType = ZuiEmit.GetBindingDataType(binding, namedControls);
            var baseType = binding.BaseType;

            sb.AppendIndentedLine(3, $"case \"{pascalName}\":");

            if (binding.IsCollection)
            {
                // Collection binding: accept the ObservableCollection type
                sb.AppendIndentedLine(4, $"if (value is {dataType} typedValue{pascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = typedValue{pascalName};");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
                sb.AppendIndentedLine(4, "return true;");
                continue;
            }

            // If binding to a named control, use the interface type for pattern matching
            var matchType = ZuiEmit.IsNamedControl(binding.Bind, namedControls) 
                ? $"I{baseType}Data" 
                : baseType;

            // Use pattern matching to handle nullable/non-nullable scenarios
            if (binding.IsNullable)
            {
                // Target is nullable - accept null or the base type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{pascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = typedValue{pascalName};");
                sb.AppendIndentedLine(4, $"else if (value is null)");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = null;");
                sb.AppendIndentedLine(4, "else");
                sb.AppendIndentedLine(5, $"throw new ArgumentException($\"Cannot assign {{{{value?.GetType().Name ?? \\\"null\\\"}}}} to '{jsonName}' (expected type: {dataType})\");");
            }
            else
            {
                // Target is non-nullable - must be the correct type
                sb.AppendIndentedLine(4, $"if (value is {matchType} typedValue{pascalName})");
                sb.AppendIndentedLine(5, $"DataContext.{pascalName} = typedValue{pascalName};");
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

    private static void GenerateDataPropertyInfoDictionary(IEnumerable<ZuiTypes.DataBinding> bindings, StringBuilder sb)
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
                var pascalName = ZuiEmit.ToPascalCase(binding.Name);
                var comma = i < bindingList.Count - 1 ? "," : "";
                var nullableStr = binding.IsNullable ? "true" : "false";
                var typeofStr = binding.IsCollection
                    ? $"typeof(global::System.Collections.ObjectModel.ObservableCollection<I{binding.BaseType}Data>)"
                    : $"typeof({binding.BaseType})";
                sb.AppendIndentedLine(2, $"[\"{pascalName}\"] = new(\"{pascalName}\", {typeofStr}, {nullableStr}){comma}");
            }

            sb.AppendIndentedLine(1, "};");
        }

        sb.Append("\r\n");
        sb.AppendIndentedLine(1, "public IReadOnlyDictionary<string, DataPropertyInfo> DataPropertyInfo => s_dataPropertyInfo;");
    }
}
