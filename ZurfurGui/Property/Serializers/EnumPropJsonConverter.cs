using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers;

/// <summary>
/// JSON converter for Prop<T>.
/// </summary>
/// <typeparam name="T">The underlying value type of the Prop.</typeparam>
public class EnumPropJsonConverter<T> : JsonConverter<EnumProp<T>>
    where T : struct
{
    public override EnumProp<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new EnumProp<T>(); // Default to null
        }

        // Deserialize the value of type T
        T? value = JsonSerializer.Deserialize<T>(ref reader, options);
        return value.HasValue ? new EnumProp<T>(value.Value) : new EnumProp<T>();
    }

    public override void Write(Utf8JsonWriter writer, EnumProp<T> value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
        }
        else
        {
            // Serialize the underlying value of type T
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}