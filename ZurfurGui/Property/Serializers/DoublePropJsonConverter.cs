using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the DoubleQ struct.
/// </summary>
internal class DoublePropJsonConverter : JsonConverter<DoubleProp>
{
    /// <summary>
    /// Reads and converts the JSON to a DoubleQ instance.
    /// </summary>
    public override DoubleProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var value))
        {
            return new DoubleProp(value);
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        throw new JsonException($"Invalid JSON token for DoubleQ: {reader.TokenType}");
    }

    /// <summary>
    /// Writes the DoubleQ instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DoubleProp value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteStringValue("NaN");
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
