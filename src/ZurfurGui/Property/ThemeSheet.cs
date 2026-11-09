using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;



namespace ZurfurGui.Property;

/// <summary>
/// Defines a named collection of theme variables loaded from a theme sheet. See
/// <see href="../../../docs/styles-and-themes.md">docs/styles-and-themes.md</see> for details.
/// </summary>
public class ThemeSheet
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; } = new();
}

