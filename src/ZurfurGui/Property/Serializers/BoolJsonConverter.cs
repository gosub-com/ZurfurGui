using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// JSON converter for bool that accepts the JSON literals true and false, and also the strings
/// "true" and "false" (any case).  Theme variables are always strings, so without this a bool
/// property could not be given a value from a theme.
/// </summary>
internal class BoolJsonConverter : JsonConverter<bool>
{
    /// <summary>
    /// Reads a bool from a JSON literal or from the string "true" or "false".
    /// </summary>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.String:
                var text = reader.GetString()?.Trim();
                if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                    return false;
                break;
        }
        throw new JsonException($"Invalid JSON value for bool: {reader.TokenType}");
    }

    /// <summary>
    /// Writes the bool as a JSON literal.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
