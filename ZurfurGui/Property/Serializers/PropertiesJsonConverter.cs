using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// Custom JSON converter for the Properties class.
/// </summary>
internal class PropertiesJsonConverter : JsonConverter<Properties>
{
    /// <summary>
    /// Reads and converts the JSON to a Properties instance.
    /// Ignores comments (i.e. properties starting with '#')
    /// </summary>
    public override Properties Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object.");
        }

        var properties = new Properties();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName == null)
                {
                    throw new JsonException("Property name cannot be null.");
                }

                if (propertyName.StartsWith("#"))
                {
                    // Ignore comments
                    reader.Read(); // Move to the value (which we will ignore)
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException("Expected a string value for comment.");
                    }
                    continue;
                }

                var propertyInfo = PropertyKeys.GetInfo(propertyName);
                if (propertyInfo == null)
                {
                    throw new JsonException($"Unknown property name: '{propertyName}'.");
                }

                reader.Read();

                var value = JsonSerializer.Deserialize(ref reader, propertyInfo.Type, options);
                if (value == null)
                {
                    throw new JsonException($"Property '{propertyName}' cannot have a null value.");
                }

                properties.SetById(propertyInfo.Id, value);
            }
        }

        return properties;
    }

    /// <summary>
    /// Writes the Properties instance to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Properties value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (key, propertyValue) in value)
        {
            if (key.Info is PropertyKeyInfo info && info.Name != null && info.Type != null)
            {
                writer.WritePropertyName(info.Name);
                JsonSerializer.Serialize(writer, propertyValue, info.Type, options);
            }
        }

        writer.WriteEndObject();
    }
}