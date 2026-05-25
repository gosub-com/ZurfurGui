using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;


/// <summary>
/// Custom JSON converter for the ThicknessProp struct.
/// </summary>
internal class ThicknessPropJsonConverter : JsonConverter<ThicknessProp>
{
    /// <summary>
    /// Reads and converts the JSON to a ThicknessProp instance.
    /// </summary>
    public override ThicknessProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DoubleProp left = null;
        DoubleProp top = null;
        DoubleProp right = null;
        DoubleProp bottom = null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                throw new JsonException("ThicknessProp string value cannot be empty.");

            if (double.TryParse(str, out var all))
            {
                return new ThicknessProp(all, all, all, all);
            }
            else
            {
                var parts = str.Split(';');
                foreach (var part in parts)
                {
                    var kv = part.Split(':');
                    if (kv.Length != 2)
                        throw new JsonException($"Invalid thickness part '{part}': expected 'key:value' format.");

                    var key = kv[0].Trim().ToLower();
                    if (!double.TryParse(kv[1], out var val))
                        throw new JsonException($"Invalid value for '{key}' in thickness string: '{kv[1]}' is not a number.");

                    switch (key)
                    {
                        case "left": left = val; break;
                        case "top": top = val; break;
                        case "right": right = val; break;
                        case "bottom": bottom = val; break;
                        case "vertical": top = bottom = val; break;
                        case "horizontal": left = right = val; break;
                        default:
                            throw new JsonException($"Unknown thickness field '{key}' in thickness string.");
                    }
                }
                return new ThicknessProp(left, top, right, bottom);

            }
        }


        throw new JsonException("Invalid JSON for ThicknessProp.");
    }

    /// <summary>
    /// Writes the ThicknessProp instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ThicknessProp value, JsonSerializerOptions options)
    {
        var parts = new List<string>();
        if (value.Left.HasValue)
            parts.Add($"left:{value.Left.Value}");
        if (value.Top.HasValue)
            parts.Add($"top:{value.Top.Value}");
        if (value.Right.HasValue)
            parts.Add($"right:{value.Right.Value}");
        if (value.Bottom.HasValue)
            parts.Add($"bottom:{value.Bottom.Value}");
        writer.WriteStringValue(string.Join(";", parts));
    }
}