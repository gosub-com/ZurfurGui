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
        DoubleProp x = null;
        DoubleProp y = null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                throw new JsonException("PointProp string value cannot be empty.");

            // Accept a single number for both x and y
            if (double.TryParse(str, out var all))
            {
                return new PointProp(all, all);
            }

            var parts = str.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length != 2)
                    throw new JsonException($"Invalid point part '{part}': expected 'key:value' format.");
                var key = kv[0].Trim().ToLower();
                if (!double.TryParse(kv[1], out var val))
                    throw new JsonException($"Invalid value for '{key}' in point string: '{kv[1]}' is not a number.");
                switch (key)
                {
                    case "x": x = val; break;
                    case "y": y = val; break;
                    default:
                        throw new JsonException($"Unknown point field '{key}' in point string.");
                }
            }
            return new PointProp(x, y);
        }
        throw new JsonException("Invalid JSON for PointProp.");
    }

    /// <summary>
    /// Writes the PointQ instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, PointProp value, JsonSerializerOptions options)
    {
        var parts = new List<string>();
        if (value.X.HasValue)
            parts.Add($"x:{value.X.Value}");
        if (value.Y.HasValue)
            parts.Add($"y:{value.Y.Value}");
        writer.WriteStringValue(string.Join(";", parts));
    }
}