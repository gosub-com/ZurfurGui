using System.Text.Json;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;

namespace ZurfurGui.Styles;

public static class StyleManager
{
    static Dictionary<string, List<SelectorProperties>> s_styleSheetCache = new();
    record SelectorProperties(string Selector, Dictionary<string, string> Properties);

    /// <summary>
    /// Register a style sheet from its JSON source.
    /// This is usually called from ZurfurMain by the generated code.
    /// TBD: Validate style sheet so it fails early instead of later at runtime (or silently for selector)
    /// </summary>
    public static void RegisterStyleSheet(string json)
    {
        // Load style sheet dictionary from JSON
        var styleSheet = JsonSerializer.Deserialize<StyleSheet>(json, Loader.JsonSerializerOptions);
        if (styleSheet == null || styleSheet.Name == null || styleSheet.Name == "")
            throw new ArgumentException("Invalid style sheet, missing name");


        // Convert to SelectorProperties (reverse order, so we can just scan forward when applying)
        var styleSheetSelectors = new List<SelectorProperties>();
        foreach (var propertySet in styleSheet.Styles.AsEnumerable().Reverse())
        {
            var selector = propertySet.GetValueOrDefault(".selectors") ?? "";
            if (selector == "")
                throw new ArgumentException($"Missing '.selectors' property key in style sheet '{styleSheet.Name}'");
            propertySet.Remove(".selectors");
            styleSheetSelectors.Add(new(selector, propertySet));
        }

        if (s_styleSheetCache.ContainsKey(styleSheet.Name))
            throw new ArgumentException($"Theme '{styleSheet.Name}' is already registered");
        s_styleSheetCache[styleSheet.Name] = styleSheetSelectors;
    }

    internal static T FindStyle<T>(View view, PropertyKey<T> key)
    {
        // Properties set by code take precedence
        IMergable<T>? mergable = null;
        if (view._properties.TryGet(key, out var value) && value is T typedValue)
        {
            if (value is IMergable<T> mergableValue)
            {
                if (mergableValue.IsComplete)
                    return value;
                mergable = mergableValue;
            }
            else
            {
                return typedValue;
            }
        }

        T styledProperty;
        if (typeof(IMergable<T>).IsAssignableFrom(typeof(T)))
        {
            foreach (var p in EnumerateStyledValues(view, key))
            {
                if (p is not IMergable<T>)
                    continue; // Skip anything with incorrect type

                if (mergable == null)
                {
                    mergable = (IMergable<T>)p;
                    continue;
                }
                var merged = mergable.Or(p);
                mergable = (IMergable<T>)merged!;
                if (mergable.IsComplete)
                    break;
            }
            styledProperty = mergable == null ? key.StyleDefault : (T)mergable;
        }
        else
        {
            styledProperty = key.StyleDefault;
            foreach (var p in EnumerateStyledValues(view, key))
            {
                styledProperty = p;
                break;
            }
        }

        return styledProperty;
    }


    /// <summary>
    /// Find style properties for a view by matching its classes against all globally active style sheets.
    /// Theme sheets (set via AppWindow.Theme) take highest priority
    /// </summary>
    static IEnumerable<T> EnumerateStyledValues<T>(View view, PropertyKey<T> key)
    {
        var classes = view._properties.Get(Panel.Classes);
        if (classes == null || classes.Count == 0)
            yield break;

        // Build iteration order: component sheets first (lowest priority), then the active theme sheet last (highest priority).
        // For non-mergable types the first yielded value wins, and we iterate in reverse, so
        // the last sheet appended here is yielded first and wins.
        var allNames = s_styleSheetCache.Keys;
        var themeName = view.AppWindow?.Theme;
        IEnumerable<string> sheets;
        if (!string.IsNullOrEmpty(themeName) && allNames.Contains(themeName))
        {
            sheets = allNames.Where(n => n != themeName).Concat([themeName]).ToList();
        }
        else
        {
            sheets = allNames;
        }

        foreach (var name in sheets.AsEnumerable().Reverse())
        {

            foreach (var selectorProperties in s_styleSheetCache[name])
            {
                if (selectorProperties.Properties.TryGetValue(key.Name, out var propertyValue)
                    && StyleMatches(view, new TextLines(selectorProperties.Selector), classes))
                {
                    // Throw if user sends wrong type
                    yield return (T)ResolveStyleSheetProperty(view, name, key.Name, propertyValue);
                }
            }
        }
    }

    static bool StyleMatches(View view, TextLines selectors, TextLines classes)
    {
        foreach (var selector in selectors)
        {
            var s = selector.Split(':');
            foreach (var c in classes)
            {
                if (c == s[0])
                {
                    // Check pseudo classes
                    var match = true;
                    for (int i = 1; i < s.Length; i++)
                    {
                        switch (s[i])
                        {
                            case "IsPointerOver":
                                if (!view.GetProperty(Panel.IsPointerOver))
                                    match = false;
                                break;
                            case "!IsPointerOver":
                                if (view.GetProperty(Panel.IsPointerOver))
                                    match = false;
                                break;
                            case "IsPressed":
                                if (!view.GetProperty(Panel.IsPressed))
                                    match = false;
                                break;
                            case "!IsPressed":
                                if (view.GetProperty(Panel.IsPressed))
                                    match = false;
                                break;
                            default:
                                match = false;
                                break;
                        }
                    }
                    return match;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Resolves a registered property name and value from a text string or variable name on the theme.
    /// </summary>
    private static object ResolveStyleSheetProperty(View view, string sheetName, string propName, string propValue)
    {
        // Find the registered property (e.g. ".padding", "TextView.color", etc.)
        var propertyInfo = PropertyKeys.GetInfo(propName);
        if (propertyInfo == null)
            throw new ArgumentException($"Invalid property key '{propName}' in style sheet '{sheetName}'");

        // Check for variable name
        if (propValue.StartsWith("@"))
        {
            return ResolveThemeVariable(view, propertyInfo, propValue.Substring(1));
        }

        // Validate deserialization of the value using Loader's JSON deserializer
        return DeserializeProperty(propName, propValue, propertyInfo.Type, $"style sheet '{sheetName}'");
    }

    private static object DeserializeProperty(
        string propName, 
        string propValue, 
        Type propertyType, 
        string debugInfoSheetName)
    {
        try
        {
            var jsonString = JsonSerializer.Serialize(propValue, Loader.JsonSerializerOptions);
            var property = JsonSerializer.Deserialize(jsonString, propertyType, Loader.JsonSerializerOptions);
            if (property == null || property.GetType() != propertyType)
                throw new ArgumentException($"Null or invalid type");
            return property;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to deserialize property '{propName}' to type '{propertyType} in '{debugInfoSheetName}'': {ex.Message}", ex);
        }
    }

    static object ResolveThemeVariable(View view, IPropertyKey propInfo, string variableExpression)
    {
        object? value = null;

        if (variableExpression.Contains("|"))
        {
            foreach (var variableName in variableExpression.Split('|'))
            {
                value = ResolveThemeVariablePartialWalk(view, propInfo, variableName.Trim(), value);
                if (value != null && IsComplete(value))
                    return value;
            }
        }
        else
        {
            // variableExpression == variableName
            value = ResolveThemeVariablePartialWalk(view, propInfo, variableExpression.Trim(), value);
        }

        if (value == null)
            throw new ArgumentException($"Variable '{variableExpression}' not found "
                +$"when looking up variable reference in property '{propInfo.Name}'");

        return value;
    }

    static bool IsComplete(object ?value)
    {
        if (value == null)
            return false;
        if (value is IMergable mergable)
            return mergable.IsComplete;
        return true;
    }

    // TBD: This will walk the theme chain
    static object? ResolveThemeVariablePartialWalk(
        View view, 
        IPropertyKey propInfo, 
        string variableName,
        object? partialValue)
    {
        // Look up in current theme first
        var theme = view.AppWindow?.Theme ?? "";
        if (theme != "")
            partialValue = ResolveThemeVariablePartial(view, propInfo, variableName, theme, partialValue); ;

        if (IsComplete(partialValue))
            return partialValue;

        // Always resolve against the base
        partialValue = ResolveThemeVariablePartial(view, propInfo, variableName, "ZurfurDefault", partialValue);

        return partialValue;
    }

    /// <summary>
    /// Resolve a partial theme variable.
    /// Returns the partialValue with additional info (or null if none supplied and none found)
    /// Throws if the theme name is invalid.
    /// </summary>
    static object? ResolveThemeVariablePartial(
        View view,
        IPropertyKey propInfo,
        string variableName,
        string theme,
        object? partialValue)
    {
        // If we already have a value and it's not mergable (or is complete), we are done
        if (IsComplete(partialValue))
            return partialValue;

        // Retrieve variable from theme
        if (!ThemeManager.RegisteredThemes.TryGetValue(theme, out var themeSheet))
            throw new ArgumentException($"Theme '{theme}' not found when looking up variable reference "
                + $"'{variableName}' in property '{propInfo.Name}'");
        if (!themeSheet.Variables.TryGetValue(variableName, out var propertyValue))
            return partialValue; // Not found

        // Deserialize
        var newValue = DeserializeProperty(propInfo.Name, propertyValue, propInfo.Type, $"theme '{theme}'");
        if (newValue == null)
            return partialValue; // No update, keep what we have
        if (partialValue == null)
            return newValue; // First found, no need to merge

        // Merge - we know it's mergable because if partialValue were not, it would exit above
        if (partialValue.GetType() != newValue.GetType())
            throw new ArgumentException($"Type mismatch when merging theme variable reference "
                +$"for property '{propInfo.Name}' in theme '{theme}'");

        return ((dynamic)partialValue).Or((dynamic)newValue);
    }


}
