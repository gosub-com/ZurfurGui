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
            string? fontName = null;
            DoubleProp fontSize = new DoubleProp();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return new FontProp(); // Default to null values
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    throw new JsonException("FontProp string value cannot be empty.");
                var parts = str.Split(';');
                foreach (var part in parts)
                {
                    var kv = part.Split(':');
                    if (kv.Length != 2)
                        throw new JsonException($"Invalid font part '{part}': expected 'key:value' format.");
                    var key = kv[0].Trim().ToLower();
                    var val = kv[1].Trim();
                    switch (key)
                    {
                        case "name": fontName = val; break;
                        case "size":
                            if (!double.TryParse(val, out var sizeVal))
                                throw new JsonException($"Invalid value for 'size' in font string: '{val}' is not a number.");
                            fontSize = sizeVal;
                            break;
                        default:
                            throw new JsonException($"Unknown font field '{key}' in font string.");
                    }
                }
                return new FontProp(fontName, fontSize);
            }
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing FontProp.");
        }

        public override void Write(Utf8JsonWriter writer, FontProp value, JsonSerializerOptions options)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(value.Name))
                parts.Add($"name:{value.Name}");
            if (value.Size.HasValue)
                parts.Add($"size:{value.Size.Value}");
            writer.WriteStringValue(string.Join(";", parts));
        }
    }
}