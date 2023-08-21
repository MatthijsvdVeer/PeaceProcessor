namespace PeaceProcessor.Functions
{
    using System.Net;
    using Azure;
    using Azure.AI.OpenAI;
    using Azure.Storage.Queues;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.DurableTask.Client;
    using Microsoft.Extensions.Logging;
    using Orchestrator;

    public class CreateTopicsFunction
    {
        private readonly QueueClient queueClient;
        private readonly OpenAIClient openAiClient;

        public CreateTopicsFunction(QueueClient queueClient, OpenAIClient openAiClient)
        {
            this.queueClient = queueClient;
            this.openAiClient = openAiClient;
        }

        [Function("CreateTopicsFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateMeditationFunction));
            var prompt = await File.ReadAllTextAsync("./Prompts/topics-prompt.txt");
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

            var completions = responseWithoutStream.Value;
            var responseContent = completions.Choices[0].Message.Content;
            // split the text on newlines  
            var lines = responseContent.Split(
                               new[] { "\r\n", "\r", "\n" },
                                              StringSplitOptions.None
                                          );
            foreach (var line in lines)
            {
                logger.LogInformation("Sending topic: {topic}", line);
                await this.queueClient.SendMessageAsync(line);
            }
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
