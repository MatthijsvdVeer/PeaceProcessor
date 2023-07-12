using System.Text.Json.Serialization;

internal sealed class OpenAiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    public OpenAiRequest(string model, double temperature)
    {
        this.Model = model;
        this.Temperature = temperature;
        this.Messages = new List<OpenAiMessage>();
    }
}