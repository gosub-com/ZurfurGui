using System.Text.Json.Serialization;

namespace ZurfurGui.Property;

public class StyleSheet
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("styles")]
    public List<Dictionary<string, string>> Styles { get; init; } = [];
}

