using System.Text.Json.Serialization;

internal sealed class OpenAiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; }

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; }
}