using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZurfurGui.Base;

namespace ZurfurGui.Base.Serializers;

/// <summary>
/// Custom JSON converter for the Properties class.
/// </summary>
internal class PropertiesJsonConverter : JsonConverter<Properties>
{
    /// <summary>
    /// Reads and converts the JSON to a Properties instance.
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
            writer.WritePropertyName(key.Name);
            JsonSerializer.Serialize(writer, propertyValue, key.Type, options);
        }

        writer.WriteEndObject();
    }
}