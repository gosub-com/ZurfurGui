using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the PointQ struct.
/// </summary>
internal class PointPropJsonConverter : JsonConverter<PointProp>
{
    /// <summary>
    /// Reads and converts the JSON to a PointQ instance.
    /// </summary>
    public override PointProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            double? x = null;
            double? y = null;

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

                    if (propertyName == "X" && reader.TokenType == JsonTokenType.Number)
                    {
                        x = reader.GetDouble();
                    }
                    else if (propertyName == "Y" && reader.TokenType == JsonTokenType.Number)
                    {
                        y = reader.GetDouble();
                    }
                }
            }

            return new PointProp(
                x.HasValue ? new DoubleProp(x.Value) : new DoubleProp(),
                y.HasValue ? new DoubleProp(y.Value) : new DoubleProp()
            );
        }

        throw new JsonException("Invalid JSON for PointQ.");
    }

    /// <summary>
    /// Writes the PointQ instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, PointProp value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.X.HasValue)
        {
            writer.WriteNumber("X", value.X.Value);
        }

        if (value.Y.HasValue)
        {
            writer.WriteNumber("Y", value.Y.Value);
        }

        writer.WriteEndObject();
    }
}