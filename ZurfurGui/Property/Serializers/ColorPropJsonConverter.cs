using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the ColorProp struct, which converts from CSS color strings.
/// </summary>
internal class ColorPropJsonConverter : JsonConverter<ColorProp>
{
    /// <summary>
    /// Reads and converts the JSON to a ColorProp instance.
    /// </summary>
    public override ColorProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var colorString = reader.GetString();
            if (string.IsNullOrWhiteSpace(colorString))
            {
                return new ColorProp(); // Null or empty string results in a null ColorProp
            }

            var color = Color.ParseCss(colorString);
            if (color.HasValue)
            {
                return new ColorProp(color.Value);
            }
            throw new JsonException($"Invalid color string: '{colorString}'");
        }

        throw new JsonException("Expected a JSON string for ColorProp.");
    }

    /// <summary>
    /// Writes the ColorProp instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ColorProp value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteStringValue("");
        }
    }
}