using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;

namespace ZurfurGui.Styles.Serializers;

/// <summary>
/// Custom JSON converter for the StyleProperty class.
/// </summary>
internal class StylePropertyJsonConverter : JsonConverter<StyleProperty>
{
    /// <summary>
    /// Reads and converts the JSON to a StyleProperty instance.
    /// </summary>
    public override StyleProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for StyleProperty.");
        }

        TextLines? selector = null;
        Properties? properties = null;

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

                if (propertyName == "Selector")
                {
                    selector = JsonSerializer.Deserialize<TextLines>(ref reader, options);
                }
                else if (propertyName == "Properties")
                {
                    properties = JsonSerializer.Deserialize<Properties>(ref reader, options);
                }
                else
                {
                    throw new JsonException($"Unknown property '{propertyName}' in StyleProperty.");
                }
            }
        }

        if (selector == null)
        {
            throw new JsonException("Missing required property 'Selector' for StyleProperty.");
        }

        return new StyleProperty
        {
            Selector = selector,
            Properties = properties ?? []
        };
    }

    /// <summary>
    /// Writes the StyleProperty instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, StyleProperty value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Selector");
        JsonSerializer.Serialize(writer, value.Selector, options);

        writer.WritePropertyName("Properties");
        JsonSerializer.Serialize(writer, value.Properties, options);

        writer.WriteEndObject();
    }
}