using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;

namespace ZurfurGui.Styles;


public static class ThemeManager
{
    private static readonly Dictionary<string, ThemeSheet> s_themes = new();

    public static IReadOnlyDictionary<string, ThemeSheet> RegisteredThemes => s_themes;


    /// <summary>
    /// Register a style sheet from its JSON source.
    /// This is usually called from ZurfurMain by the generated code.
    /// TBD: Validate theme so it fails early instead later at runtime
    /// </summary>
    public static void RegisterTheme(string json)
    {
        // Deserialize and register the theme (throw exception if it already exists)
        var theme = JsonSerializer.Deserialize<ThemeSheet>(json, Loader.JsonSerializerOptions);
        if (theme == null || theme.Name == "")
            throw new ArgumentException("Invalid theme, missing name");
        if (s_themes.ContainsKey(theme.Name))
            throw new ArgumentException($"Theme '{theme.Name}' is already registered");
        s_themes[theme.Name] = theme;
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

        // Theme token?
        if (view._properties.TryGet(Panel.ThemeTokens, out var themeTokens)
            && themeTokens != null
            && themeTokens.TryGetValue(key.Name, out var tokenValue)
            && tokenValue != null)
        {
            // Throw if theme has wrong variable type
            var themeVar = (T)ResolveThemeVariable(view, key, tokenValue);

            // Merge property if necessary
            if (themeVar is IMergable<T> m && mergable != null)
                return mergable.Or(themeVar);
            else
                return themeVar;

        }

        if (typeof(IMergable<T>).IsAssignableFrom(typeof(T)))
            return mergable == null ? key.StyleDefault : (T)mergable;
        else
            return key.StyleDefault;
    }

    /// <summary>
    /// Resolve a theme token expression in the form "token1|token2" where each token is a variable name in the theme.
    /// Fully resolves "token1", then if there are any unresolved parts continues on to "token2"
    /// </summary>
    static object ResolveThemeVariable(View view, IPropertyKey propInfo, string variableExpression)
    {
        object? value = null;

        if (variableExpression.Contains("|"))
        {
            foreach (var variableName in variableExpression.Split('|'))
            {
                value = ResolveThemeVariableWalk(view, propInfo, variableName.Trim(), value);
                if (value != null && IsComplete(value))
                    return value;
            }
        }
        else
        {
            value = ResolveThemeVariableWalk(view, propInfo, variableExpression.Trim(), value);
        }

        if (value == null)
            throw new ArgumentException($"Variable '{variableExpression}' not found "
                + $"when looking up variable reference in property '{propInfo.Name}'");

        return value;
    }

    /// <summary>
    /// Returns true if the object is complete (not null and either not mergable or mergable and complete)
    /// </summary>
    public static bool IsComplete(object? value)
    {
        if (value == null)
            return false;
        if (value is IMergable mergable)
            return mergable.IsComplete;
        return true;
    }

    /// <summary>
    /// Merges two objects of the same type. If either is null, returns the other. If objects are mergable, calls Or() on them. Otherwise returns the first value.
    /// </summary>
    public static object? Merge(object? value1, object? value2)
    {
        if (value1 == null)
            return value2;
        if (value2 == null)
            return value1;
        if (value1.GetType() != value2.GetType())
            throw new ArgumentException($"Cannot merge values of different types: {value1.GetType()} and {value2.GetType()}");
        if (value1 as IMergable == null)
            return value1;
        return ((dynamic)value1).Or((dynamic)value2);
    }

    /// <summary>
    /// Look up a theme token on the given view. 
    /// Walk up the view chain to find active themes, then resolve against "ZurfurDefault" and "ZurfurBase" if not found.
    /// </summary>
    static object? ResolveThemeVariableWalk(
        View view,
        IPropertyKey propInfo,
        string variableName,
        object? partialValue)
    {
        // Walk up view chain to find active themes
        View? viewMaybeNull = view;
        while (viewMaybeNull != null)
        {
            if (viewMaybeNull.ActiveThemes is TextLines themes)
            {
                // Walk through themes in order, first theme has highest priority
                foreach (var theme in themes)
                {
                    partialValue = ResolveThemeVariableOnTheme(view, propInfo, variableName, theme, partialValue);
                    if (IsComplete(partialValue))
                        return partialValue;
                }
            }
            if (IsComplete(partialValue))
                return partialValue;
            viewMaybeNull = viewMaybeNull.Parent;
        }



        // Always resolve against the base
        partialValue = ResolveThemeVariableOnTheme(view, propInfo, variableName, "ZurfurDefault", partialValue);
        partialValue = ResolveThemeVariableOnTheme(view, propInfo, variableName, "ZurfurBase", partialValue);

        return partialValue;
    }

    /// <summary>
    /// Resolve a partial theme variable.
    /// Returns the partialValue with additional info (or null if none supplied and none found)
    /// Throws if the theme name is invalid.
    /// </summary>
    static object? ResolveThemeVariableOnTheme(
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
        if (!RegisteredThemes.TryGetValue(theme, out var themeSheet))
            throw new ArgumentException($"Theme '{theme}' not found when looking up variable reference "
                + $"'{variableName}' in property '{propInfo.Name}'");
        if (!themeSheet.Variables.TryGetValue(variableName, out var propertyValue))
            return partialValue; // Not found

        // Split token expression
        foreach (var themeExpression in propertyValue.Split(';'))
        {
            if (themeExpression.Contains('?'))
            {
                var condParts = themeExpression.Split('?');
                if (ThemeConditionMatches(view, condParts[0].Trim()))
                {
                    var newValue = DeserializeProperty(propInfo.Name, condParts[1].Trim(), propInfo.Type, $"theme '{theme}'");
                    partialValue = Merge(partialValue, newValue);
                }
            }
            else if (themeExpression.Trim() != "")
            {
                var newValue = DeserializeProperty(propInfo.Name, themeExpression.Trim(), propInfo.Type, $"theme '{theme}'");
                partialValue = Merge(partialValue, newValue);
            }

            if (IsComplete(partialValue))
                return partialValue;

        }
        return partialValue;
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

    static bool ThemeConditionMatches(View view, string themeCondition)
    {
        var isNegated = themeCondition.StartsWith("!");
        var conditionName = isNegated ? themeCondition[1..] : themeCondition;
        bool conditionValue;

        switch (conditionName)
        {
            case "isPointerOver":
                conditionValue = view.GetProperty(Panel.IsPointerOver);
                break;
            case "isPressed":
                conditionValue = view.GetProperty(Panel.IsPressed);
                break;
            default:
                if (PropertyKeys.GetInfo(conditionName) is not PropertyKey<bool> property
                    || !property.OwnerType.IsAssignableFrom(view.Controller.GetType()))
                {
                    throw new ArgumentException($"Invalid theme condition '{themeCondition}'");
                }

                conditionValue = view.GetProperty(property);
                break;
        }

        return isNegated ? !conditionValue : conditionValue;
    }


}
