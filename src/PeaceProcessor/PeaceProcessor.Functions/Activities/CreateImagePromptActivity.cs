namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.AI.OpenAI;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using System.Text;

    internal sealed class CreateImagePromptActivity
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly OpenAIClient openAiClient;

        public CreateImagePromptActivity(BlobContainerClient blobContainerClient, OpenAiClientFactory openAiClientFactory)
        {
            this.blobContainerClient = blobContainerClient;
            this.openAiClient = openAiClientFactory.Create(OpenAiKind.Chat);
        }

        [Function(nameof(CreateImagePromptActivity))]
        public async Task<string> Run([ActivityTrigger] CreateScriptContext createScriptContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateImagePromptActivity));
            logger.LogInformation("Creating image prompt for topic: {topic}", createScriptContext.Topic);
            var prompt = await File.ReadAllTextAsync("Prompts/image-prompt.txt");
            prompt = prompt.Replace("{{TOPIC}}", createScriptContext.Topic);

            Response<ChatCompletions> responseWithoutStream = await this.openAiClient.GetChatCompletionsAsync(
                "gpt-4",
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, prompt)
                    },
                    Temperature = (float)0.7,
                    MaxTokens = 2000,
                    NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                });

            ChatCompletions completions = responseWithoutStream.Value;
            var imagePrompt = completions.Choices[0].Message.Content;

            var blobPath = $"{createScriptContext.Timestamp}/image-prompt.txt";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);

            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(imagePrompt)));
            return blobPath;
        }
    }
}