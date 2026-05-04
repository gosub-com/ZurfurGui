using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using ZurfurGui.Base;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the TextLines class, which accepts both a string and an array of strings.
/// </summary>
internal class TextLinesJsonConverter : JsonConverter<TextLines>
{
    /// <summary>
    /// Reads and converts the JSON to a TextLines instance.
    /// </summary>
    public override TextLines Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Deserialize a single string into TextLines
            return new TextLines(reader.GetString() ?? string.Empty);
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Deserialize an array of strings into TextLines
            var lines = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    lines.Add(reader.GetString() ?? string.Empty);
                }
                else
                {
                    throw new JsonException("Invalid JSON token in TextLines array.");
                }
            }

            return new TextLines(lines);
        }

        throw new JsonException("Invalid JSON for TextLines.");
    }

    /// <summary>
    /// Writes the TextLines instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TextLines value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            // Serialize as a single string if there is only one line
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            // Serialize as an array of strings if there are multiple lines
            writer.WriteStartArray();
            foreach (var line in value)
                writer.WriteStringValue(line);
            writer.WriteEndArray();
        }
    }
}