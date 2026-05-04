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
        Dictionary<string, JsonElement>? dataProperties = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                if (propertyName == null || propertyName == "")
                {
                    throw new JsonException("Property name cannot be null.");
                }

                // Skip comments (properties starting with '#')
                if (propertyName.StartsWith("#"))
                {
                    reader.Read();
                    reader.Skip();
                    continue;
                }

                var propertyInfo = PropertyKeys.GetInfo(propertyName);

                reader.Read();

                // Unknown or data property?
                if (propertyInfo == null)
                {
                    // Verify it's a data property (must not contain a '.')
                    if (propertyName.Contains("."))
                        throw new JsonException($"Unknown property name: '{propertyName}' instead");

                    // Accumulate unknown data property as JsonElement
                    dataProperties ??= new Dictionary<string, JsonElement>();
                    var jsonElement = JsonDocument.ParseValue(ref reader).RootElement.Clone();
                    dataProperties[propertyName] = jsonElement;
                    continue;
                }

                var value = JsonSerializer.Deserialize(ref reader, propertyInfo.Type, options);
                if (value == null)
                {
                    throw new JsonException($"Property '{propertyName}' cannot have a null value.");
                }

                properties.SetById(propertyInfo.Id, value);
            }
        }

        // Store accumulated data properties using Panel.DataProperties key
        if (dataProperties != null && dataProperties.Count > 0)
        {
            var dataPropertiesKey = PropertyKeys.GetInfo(".dataProperties");
            if (dataPropertiesKey != null)
            {
                properties.SetById(dataPropertiesKey.Id, dataProperties);
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
                // Special handling for .dataProperties - expand inline instead of nesting
                if (info.Name == ".dataProperties" && propertyValue is Dictionary<string, JsonElement> dataProps)
                {
                    foreach (var (name, jsonElement) in dataProps)
                    {
                        writer.WritePropertyName(name);
                        jsonElement.WriteTo(writer);
                    }
                }
                else
                {
                    writer.WritePropertyName(info.Name);
                    JsonSerializer.Serialize(writer, propertyValue, info.Type, options);
                }
            }
        }

        writer.WriteEndObject();
    }
}