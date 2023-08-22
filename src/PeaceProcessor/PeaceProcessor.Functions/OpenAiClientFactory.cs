namespace PeaceProcessor.Functions
{
    using Azure;
    using Azure.AI.OpenAI;
    using Microsoft.Extensions.Configuration;

    public sealed class OpenAiClientFactory
    {
        private readonly IConfiguration configuration;

        private Dictionary<OpenAiKind, OpenAiSettings> configurations = new Dictionary<OpenAiKind, OpenAiSettings>
        {
            {OpenAiKind.Image, new OpenAiSettings("openai_image_endpoint", "openai_image_key")},
            {OpenAiKind.Chat, new OpenAiSettings("openai_chat_endpoint", "openai_chat_key")}
        };

        public OpenAiClientFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public OpenAIClient Create(OpenAiKind kind)
        {
            if (this.configurations.TryGetValue(kind, out var settings))
            {
                return new OpenAIClient(new Uri(this.configuration[settings.Endpoint]), new AzureKeyCredential(this.configuration[settings.Key]));
            }

            throw new ArgumentException($"No configuration found for {kind}");
        }
    }
}