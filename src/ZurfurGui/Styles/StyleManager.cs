using System.Text.Json;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;

namespace ZurfurGui.Styles;

public static class StyleManager
{
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

    static object? Merge(object ?value1, object? value2)
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
        partialValue = ResolveThemeVariablePartial(view, propInfo, variableName, "ZurfurBase", partialValue);

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
        switch (themeCondition)
        {
            case "isHover":
                return view.GetProperty(Panel.IsPointerOver);
            case "!isHover":
                return !view.GetProperty(Panel.IsPointerOver);
            case "isPressed":
                return view.GetProperty(Panel.IsPressed);
            case "!isPressed":
                return !view.GetProperty(Panel.IsPressed);
            default:
                throw new ArgumentException($"Invalid theme condition '{themeCondition}'");
        }
    }

}
