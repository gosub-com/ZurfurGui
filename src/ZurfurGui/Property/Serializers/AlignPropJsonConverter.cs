using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for AlignProp that supports both JSON object and string formats.
/// </summary>
internal class AlignPropJsonConverter : JsonConverter<AlignProp>
{
    public override AlignProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        AlignHorizontal? horizontal = null;
        AlignVertical? vertical = null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                throw new JsonException("AlignProp string value cannot be empty.");
            // Do not accept a single value (e.g. "center") or a number
            if (!str.Contains(":"))
                throw new JsonException("AlignProp string must be in 'horizontal:Center;vertical:Top' format.");
            var parts = str.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length != 2)
                    throw new JsonException($"Invalid align part '{part}': expected 'key:value' format.");
                var key = kv[0].Trim().ToLower();
                var val = kv[1].Trim();
                switch (key)
                {
                    case "horizontal":
                        if (!Enum.TryParse<AlignHorizontal>(val, true, out var h))
                            throw new JsonException($"Invalid value for horizontal: '{val}'");
                        horizontal = h;
                        break;
                    case "vertical":
                        if (!Enum.TryParse<AlignVertical>(val, true, out var v))
                            throw new JsonException($"Invalid value for vertical: '{val}'");
                        vertical = v;
                        break;
                    default:
                        throw new JsonException($"Unknown align field '{key}' in align string.");
                }
            }
            return new AlignProp(horizontal, vertical);
        }
        throw new JsonException("Invalid JSON for AlignProp.");
    }

    public override void Write(Utf8JsonWriter writer, AlignProp value, JsonSerializerOptions options)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (value.Horizontal.HasValue)
            parts.Add($"horizontal:{value.Horizontal.Value}");
        if (value.Vertical.HasValue)
            parts.Add($"vertical:{value.Vertical.Value}");
        writer.WriteStringValue(string.Join(";", parts));
    }
}
