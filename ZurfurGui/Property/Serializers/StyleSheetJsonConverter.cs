using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the StyleSheet class.
/// </summary>
internal class StyleSheetJsonConverter : JsonConverter<StyleSheet>
{
    /// <summary>
    /// Reads and converts the JSON to a StyleSheet instance.
    /// </summary>
    public override StyleSheet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for StyleSheet.");
        }

        string? name = null;
        StyleProperty[]? styles = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                if (propertyName == "Name")
                {
                    name = reader.GetString();
                }
                else if (propertyName == "Styles")
                {
                    styles = JsonSerializer.Deserialize<StyleProperty[]>(ref reader, options);
                }
                else
                {
                    throw new JsonException($"Unknown property '{propertyName}' in StyleSheet.");
                }
            }
        }

        return new StyleSheet
        {
            Name = name ?? "",
            Styles = styles ?? []
        };
    }

    /// <summary>
    /// Writes the StyleSheet instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, StyleSheet value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Name");
        writer.WriteStringValue(value.Name);

        writer.WritePropertyName("Styles");
        JsonSerializer.Serialize(writer, value.Styles, options);

        writer.WriteEndObject();
    }
}