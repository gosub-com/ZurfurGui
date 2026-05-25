using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the SizeProp struct.
/// </summary>
internal class SizePropJsonConverter : JsonConverter<SizeProp>
{
    /// <summary>
    /// Reads and converts the JSON to a SizeProp instance.
    /// </summary>
    public override SizeProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DoubleProp width = null;
        DoubleProp height = null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                throw new JsonException("SizeProp string value cannot be empty.");

            // Accept a single number for both width and height
            if (double.TryParse(str, out var all))
            {
                return new SizeProp(all, all);
            }
            var parts = str.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length != 2)
                    throw new JsonException($"Invalid size part '{part}': expected 'key:value' format.");
                var key = kv[0].Trim().ToLower();
                if (!double.TryParse(kv[1], out var val))
                    throw new JsonException($"Invalid value for '{key}' in size string: '{kv[1]}' is not a number.");
                switch (key)
                {
                    case "width": width = val; break;
                    case "height": height = val; break;
                    default:
                        throw new JsonException($"Unknown size field '{key}' in size string.");
                }
            }
            return new SizeProp(width, height);
        }
        throw new JsonException("Invalid JSON for SizeProp.");
    }

    /// <summary>
    /// Writes the SizeProp instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, SizeProp value, JsonSerializerOptions options)
    {
        var parts = new List<string>();
        if (value.Width.HasValue)
            parts.Add($"width:{value.Width.Value}");
        if (value.Height.HasValue)
            parts.Add($"height:{value.Height.Value}");
        writer.WriteStringValue(string.Join(";", parts));
    }
}