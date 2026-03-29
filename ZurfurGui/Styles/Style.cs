using System;
using System.Collections.Generic;
using System.Text;
using ZurfurGui.Base;
using ZurfurGui.Controls;
using ZurfurGui.Property;

namespace ZurfurGui.Styles;

internal static class Style
{
    // TBD: Should we be caching style lookups in the properties?
    internal const int PROPERTY_STYLE_CACHE_BEGIN = 10000;
    internal const int PROPERTY_STYLE_CACHE_END = 20000;


    internal static void ClearStyleCacheInternal(this View view)
    {
        var keysToRemove = view._properties
            .Select(k => k.key)
            .Where(k => k.IdAsInt >= PROPERTY_STYLE_CACHE_BEGIN && k.IdAsInt < PROPERTY_STYLE_CACHE_END)
            .ToList();
        foreach (var key in keysToRemove)
            view._properties.RemoveById(key);
    }

    internal static T GetStyle<T>(View view, PropertyKey<T> key)
    {
        // Properties cached from style lookup below
        if (view._properties.TryGetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN),
                out var styledValue) && styledValue is T typedStyledValue)
        {
            return typedStyledValue;
        }

        var styledProperty = FindStyle(view, key);

        // Cache the styled property for quick lookup above
        view._properties.SetById(new PropertyKeyId(key.IdAsInt + PROPERTY_STYLE_CACHE_BEGIN), styledProperty!);

        return styledProperty;
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
    /// Walk up the view tree to find style properties based on the classes property.
    /// </summary>
    static IEnumerable<T> EnumerateStyledValues<T>(View view, PropertyKey<T> key)
    {
        var classes = view._properties.Get(Panel.Classes);
        if (classes == null || classes.Count == 0)
            yield break;

        // Walk up the view tree
        for (var parent = view; parent != null; parent = parent.Parent)
        {
            if (!parent._properties.TryGet(Panel.UseStyles, out var useStyles) || useStyles == null)
                continue;

            foreach (var useStyle in useStyles.Reverse())
            {
                if (Loader.GetStyleSheet(useStyle) is not StyleSheet styleSheet)
                    continue;

                foreach (var style in styleSheet.Styles.Reverse())
                {
                    if (style.TryGet(Panel.Selectors, out var selectors) && selectors != null
                        && style.TryGet(key, out var p) && p != null
                        && StyleMatches(view, selectors, classes))
                    {
                        yield return p;
                    }
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
                            case "IsDarkMode":
                                if (!view.AppWindow?.IsDarkMode ?? false)
                                    match = false;
                                break;
                            case "!IsDarkMode":
                                if (view.AppWindow?.IsDarkMode ?? false)
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

}
