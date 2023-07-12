using System.Text.Json.Serialization;

internal class OpenAiMessage
{
    [JsonPropertyName("role")]
    public OpenAiRole Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    public OpenAiMessage(OpenAiRole role, string content)
    {
        this.Role = role;
        this.Content = content;
    }
}