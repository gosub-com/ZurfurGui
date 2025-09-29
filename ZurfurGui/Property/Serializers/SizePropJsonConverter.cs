using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the SizeQ struct.
/// </summary>
internal class SizePropJsonConverter : JsonConverter<SizeProp>
{
    /// <summary>
    /// Reads and converts the JSON to a SizeQ instance.
    /// </summary>
    public override SizeProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            double? width = null;
            double? height = null;

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

                    if (propertyName == "Width" && reader.TokenType == JsonTokenType.Number)
                    {
                        width = reader.GetDouble();
                    }
                    else if (propertyName == "Height" && reader.TokenType == JsonTokenType.Number)
                    {
                        height = reader.GetDouble();
                    }
                }
            }

            return new SizeProp(
                width.HasValue ? new DoubleProp(width.Value) : new DoubleProp(),
                height.HasValue ? new DoubleProp(height.Value) : new DoubleProp()
            );
        }

        throw new JsonException("Invalid JSON for SizeQ.");
    }

    /// <summary>
    /// Writes the SizeQ instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, SizeProp value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Width.HasValue)
        {
            writer.WriteNumber("Width", value.Width.Value);
        }

        if (value.Height.HasValue)
        {
            writer.WriteNumber("Height", value.Height.Value);
        }

        writer.WriteEndObject();
    }
}