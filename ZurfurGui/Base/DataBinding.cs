using System.Text.Json.Serialization;

namespace ZurfurGui.Base;

public record class DataBinding(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("bind")] string Bind,
    [property: JsonPropertyName("default")] string Default
);
