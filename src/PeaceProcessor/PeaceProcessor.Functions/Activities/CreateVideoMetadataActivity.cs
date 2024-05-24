namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.AI.OpenAI;
    using Azure;

    internal sealed class CreateVideoMetadataActivity
    {
        private readonly OpenAIClient openAiClient;

        public CreateVideoMetadataActivity(OpenAiClientFactory aiClientFactory)
        {
            this.openAiClient = aiClientFactory.Create(OpenAiKind.Chat);
        }

        [Function(nameof(CreateVideoMetadataActivity))]
        public async Task<CreateVideoMetadataResponse> Run(
            [ActivityTrigger] CreateVideoMetadataContext createVideoMetadataContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateVideoMetadataActivity));

            logger.LogInformation("Creating video metadata for topic {topic}", createVideoMetadataContext.Topic);

            var videoDescription = await this.GetPromptResponse(createVideoMetadataContext,
                createVideoMetadataContext.Script, "Prompts/video-description.txt");
            var videoTitle = await this.GetPromptResponse(createVideoMetadataContext, createVideoMetadataContext.Script,
                "Prompts/video-title.txt");

            return new CreateVideoMetadataResponse(videoTitle, videoDescription);
        }

        private async Task<string> GetPromptResponse(CreateVideoMetadataContext createVideoMetadataContext,
            string script, string promptPath)
        {
            var prompt = await File.ReadAllTextAsync(promptPath);

            Response<ChatCompletions> response = await this.openAiClient.GetChatCompletionsAsync(
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(prompt),
                        new ChatRequestUserMessage(
                            $"The topic is '{createVideoMetadataContext.Topic}'. Here's the script: '{script}'")
                    },
                    Temperature = (float) 0.4,
                    MaxTokens = 2000,
                    NucleusSamplingFactor = (float) 0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    DeploymentName = "gpt-35-turbo"
                });

            return response.Value.Choices[0].Message.Content;
        }
    }
}