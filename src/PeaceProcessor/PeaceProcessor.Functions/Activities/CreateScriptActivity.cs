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
            string content = @"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-US'>
  <voice name='en-US-JennyNeural'>
    <mstts:express-as style=""hopeful"">
      <p>";
            Response<ChatCompletions> responseWithoutStream = await this.openAiClient.GetChatCompletionsAsync(
                "gpt-4",
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatMessage(ChatRole.System, prompt),
                        new ChatMessage(ChatRole.User, $"Today's topic will be {createScriptContext.Topic}"),
                        new ChatMessage(ChatRole.Assistant, content),
                    },
                    Temperature = (float) 0.7,
                    MaxTokens = 800,
                    NucleusSamplingFactor = (float) 0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                });

            ChatCompletions completions = responseWithoutStream.Value;
            string xml = content + completions.Choices[0].Message.Content;
            ;

            string blobPath = $"{createScriptContext.Timestamp}/script.xml";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
            return blobPath;
        }
    }
}