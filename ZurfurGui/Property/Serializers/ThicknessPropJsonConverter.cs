using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;


/// <summary>
/// Custom JSON converter for the ThicknessQ struct.
/// </summary>
internal class ThicknessPropJsonConverter : JsonConverter<ThicknessProp>
{
    /// <summary>
    /// Reads and converts the JSON to a ThicknessQ instance.
    /// </summary>
    public override ThicknessProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            double? left = null;
            double? top = null;
            double? right = null;
            double? bottom = null;

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

                    if (propertyName == "Left" && reader.TokenType == JsonTokenType.Number)
                    {
                        left = reader.GetDouble();
                    }
                    else if (propertyName == "Top" && reader.TokenType == JsonTokenType.Number)
                    {
                        top = reader.GetDouble();
                    }
                    else if (propertyName == "Right" && reader.TokenType == JsonTokenType.Number)
                    {
                        right = reader.GetDouble();
                    }
                    else if (propertyName == "Bottom" && reader.TokenType == JsonTokenType.Number)
                    {
                        bottom = reader.GetDouble();
                    }
                }
            }

            return new ThicknessProp(
                left.HasValue ? new DoubleProp(left.Value) : new DoubleProp(),
                top.HasValue ? new DoubleProp(top.Value) : new DoubleProp(),
                right.HasValue ? new DoubleProp(right.Value) : new DoubleProp(),
                bottom.HasValue ? new DoubleProp(bottom.Value) : new DoubleProp()
            );
        }

        throw new JsonException("Invalid JSON for ThicknessQ.");
    }

    /// <summary>
    /// Writes the ThicknessQ instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ThicknessProp value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Left.HasValue)
        {
            writer.WriteNumber("Left", value.Left.Value);
        }

        if (value.Top.HasValue)
        {
            writer.WriteNumber("Top", value.Top.Value);
        }

        if (value.Right.HasValue)
        {
            writer.WriteNumber("Right", value.Right.Value);
        }

        if (value.Bottom.HasValue)
        {
            writer.WriteNumber("Bottom", value.Bottom.Value);
        }

        writer.WriteEndObject();
    }
}