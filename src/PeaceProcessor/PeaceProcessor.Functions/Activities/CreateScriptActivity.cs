namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using System.Text;
    using Azure;
    using Azure.AI.OpenAI;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.Configuration;

    internal sealed class CreateScriptActivity
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly OpenAIClient openAiClient;
        private readonly string model;
        private readonly float temperature;
        private readonly int maxTokens;

        public CreateScriptActivity(BlobContainerClient blobContainerClient, OpenAiClientFactory openAiClientFactory, IConfiguration configuration)
        {
            this.blobContainerClient = blobContainerClient;
            this.openAiClient = openAiClientFactory.Create(OpenAiKind.Chat);
            this.model = configuration["model"];
            this.temperature = float.Parse(configuration["temperature"]);
            this.maxTokens = int.Parse(configuration["max_tokens"]);
        }

        [Function(nameof(CreateScriptActivity))]
        public async Task<string> Run([ActivityTrigger] CreateScriptContext createScriptContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateScriptActivity));
            logger.LogInformation("Creating script for topic: {topic}", createScriptContext.Topic);

            var prompt = await File.ReadAllTextAsync("Prompts/script-prompt.txt");
            Response<ChatCompletions> responseWithoutStream = await this.openAiClient.GetChatCompletionsAsync(
                this.model,
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, prompt),
                        new ChatMessage(ChatRole.User, createScriptContext.Topic)
                    },
                    Temperature = this.temperature,
                    MaxTokens = this.maxTokens,
                    NucleusSamplingFactor = (float) 0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                });

            ChatCompletions completions = responseWithoutStream.Value;
            var rawScript = completions.Choices[0].Message.Content;

            // Replace '<BREAK10>' with '<break time="10s"/>'. The number should remain the same
            var script = rawScript.Replace("<BREAK", "<break time=\"").Replace(">", "s\"/>");
            var ssml = await File.ReadAllTextAsync("empty.xml");
            ssml = ssml.Replace("{{SCRIPT}}", script);

            var blobPath = $"{createScriptContext.StoragePath}/script.xml";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);

            // Store the topic in the blob metadata.
            Dictionary<string, string> metadata = new()
            {
                { "topic", StringUtility.FormatForTopicMetadata(createScriptContext.Topic) }
            };

            var blobUploadOptions = new BlobUploadOptions
            {
                Metadata = metadata
            };

            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(ssml)), blobUploadOptions);
            return ssml;
        }
    }
}