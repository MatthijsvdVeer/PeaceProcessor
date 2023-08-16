namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Text;
    using Azure;
    using Azure.AI.OpenAI;
    using Azure.Storage.Blobs;

    internal sealed class CreateScriptActivity
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly OpenAIClient openAiClient;

        public CreateScriptActivity(BlobContainerClient blobContainerClient, OpenAIClient openAiClient)
        {
            this.blobContainerClient = blobContainerClient;
            this.openAiClient = openAiClient;
        }

        [Function(nameof(CreateScriptActivity))]
        public async Task<string> Run([ActivityTrigger] CreateScriptContext createScriptContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateScriptActivity));
            logger.LogInformation("Creating script for topic: {topic}", createScriptContext.Topic);

            var prompt = await File.ReadAllTextAsync("prompt.txt");
            prompt = prompt.Replace("{{TOPIC}}", createScriptContext.Topic);
            
            Response<ChatCompletions> responseWithoutStream = await this.openAiClient.GetChatCompletionsAsync(
                "gpt-4",
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, prompt)
                    },
                    Temperature = (float) 0.7,
                    MaxTokens = 2000,
                    NucleusSamplingFactor = (float) 0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                });

            ChatCompletions completions = responseWithoutStream.Value;
            var rawScript = completions.Choices[0].Message.Content;

            // replace '<BREAK10>' with '<break time="10s"/>'. The number should remain the same
            var script = rawScript.Replace("<BREAK", "<break time=\"").Replace(">", "s\"/>");
            var ssml = await File.ReadAllTextAsync("empty.xml");
            ssml = ssml.Replace("{{SCRIPT}}", script);

            string blobPath = $"{createScriptContext.Timestamp}/script.xml";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(ssml)));
            return blobPath;
        }
    }
}