using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZurfurGui.Property.Serializers
{
    /// <summary>
    /// JSON converter for BoolProp.
    /// </summary>
    public class BoolPropJsonConverter : JsonConverter<BoolProp>
    {
        public override BoolProp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new BoolProp(); // Default to null
            }

            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                return new BoolProp(reader.GetBoolean());
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing BoolProp.");
        }

        public override void Write(Utf8JsonWriter writer, BoolProp value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBooleanValue(value.Value);
            }
        }
    }
}