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

}
