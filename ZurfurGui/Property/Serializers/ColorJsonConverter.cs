using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the Color struct, which converts from CSS color strings.
/// </summary>
internal class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var colorString = reader.GetString();
            if (string.IsNullOrWhiteSpace(colorString))
                throw new JsonException("Color cannot be null or empty.");

            var color = Color.ParseCss(colorString);
            if (color.HasValue)
                return color.Value;

            throw new JsonException($"Invalid color string: '{colorString}'");
        }

        throw new JsonException("Expected a JSON string for Color.");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.CssColor);
    }
}
