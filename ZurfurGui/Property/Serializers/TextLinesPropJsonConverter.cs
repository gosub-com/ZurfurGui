using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;
using ZurfurGui.Property;

namespace ZurfurGui.Property.Serializers
{
    /// <summary>
    /// JSON converter for TextLinesProp, which accepts both a string and an array of strings.
    /// </summary>
    public class TextLinesPropJsonConverter : JsonConverter<TextLinesProp>
    {
        public override TextLinesProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new TextLinesProp(); // Default to null
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                // Deserialize a single string into TextLinesProp
                string text = reader.GetString()!;
                return new TextLinesProp(text);
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Deserialize an array of strings into TextLinesProp
                var lines = JsonSerializer.Deserialize<string[]>(ref reader, options);
                return new TextLinesProp(lines!);
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing TextLinesProp.");
        }

        public override void Write(Utf8JsonWriter writer, TextLinesProp value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
            }
            else
            {
                // Serialize TextLines as an array of strings
                var textLines = value.Value;
                writer.WriteStartArray();
                foreach (var line in textLines)
                {
                    writer.WriteStringValue(line);
                }
                writer.WriteEndArray();
            }
        }
    }
}