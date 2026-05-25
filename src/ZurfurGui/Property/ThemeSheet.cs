using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property;

public class ThemeSheet
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; } = new();
}

