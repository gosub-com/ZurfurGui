using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Property;

namespace ZurfurGui.Property.Serializers
{
    /// <summary>
    /// JSON converter for FontProp.
    /// </summary>
    public class FontPropJsonConverter : JsonConverter<FontProp>
    {
        public override FontProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new FontProp(); // Default to null values
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string? fontName = null;
                DoubleProp fontSize = new DoubleProp();

                // Read the object properties
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString()!;
                        reader.Read();

                        if (propertyName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            fontName = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (propertyName.Equals("Size", StringComparison.OrdinalIgnoreCase))
                        {
                            fontSize = reader.TokenType == JsonTokenType.Number ? new DoubleProp(reader.GetDouble()) : new DoubleProp();
                        }
                    }
                }

                return new FontProp(fontName, fontSize);
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing FontProp.");
        }

        public override void Write(Utf8JsonWriter writer, FontProp value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Serialize FontName
            if (value.Name != null)
            {
                writer.WriteString("Name", value.Name);
            }
            else
            {
                writer.WriteNull("Name");
            }

            // Serialize FontSize
            if (value.Size.HasValue)
            {
                writer.WriteNumber("Size", value.Size.Value);
            }
            else
            {
                writer.WriteNull("Size");
            }

            writer.WriteEndObject();
        }
    }
}