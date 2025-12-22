using System.Text.Json.Serialization;

namespace ZurfurGui.Property;

/// <summary>
/// This is like a css file
/// </summary>
public class StyleSheet
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("styles")]
    public Properties[] Styles { get; init; } = [];
}


