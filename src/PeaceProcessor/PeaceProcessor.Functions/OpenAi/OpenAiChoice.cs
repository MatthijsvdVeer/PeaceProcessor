using System.Text.Json.Serialization;

internal sealed class OpenAiChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public OpenAiMessage Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}