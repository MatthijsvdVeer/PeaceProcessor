using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class OpenAiRoleEnumJsonConverter : JsonConverter<OpenAiRole>
{
    public override OpenAiRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var enumString = reader.GetString();
        return Enum.Parse<OpenAiRole>(enumString, true);
    }

    public override void Write(Utf8JsonWriter writer, OpenAiRole value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}