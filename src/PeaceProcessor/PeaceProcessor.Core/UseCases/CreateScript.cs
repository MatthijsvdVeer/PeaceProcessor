namespace PeaceProcessor.Core.UseCases
{
    using Azure;
    using Azure.AI.OpenAI;

    public class CreateScript(
        OpenAIClient openAiClient,
        CreateScriptOptions options,
        CancellationToken cancellationToken)
    {
        private const string ScriptPath = "script.txt";
        private readonly string deploymentName = options.DeploymentName;
        private readonly float temperature = options.Temperature;
        private readonly int maxTokens = options.MaxTokens;

        public async Task<string> ExecuteAsync()
        {
            return await this.ExecuteAsync(options.Topic);
        }
        
        public async Task<string> ExecuteAsync(string topic)
        {
            Console.WriteLine($"Creating script for topic: {topic}");

            var prompt = await File.ReadAllTextAsync("Prompts/script-prompt.txt");
            Response<ChatCompletions> responseWithoutStream = await openAiClient.GetChatCompletionsAsync(
                new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(prompt),
                        new ChatRequestUserMessage(topic)
                    },
                    Temperature = this.temperature,
                    MaxTokens = this.maxTokens,
                    NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    DeploymentName = this.deploymentName
                }, cancellationToken);

            ChatCompletions completions = responseWithoutStream.Value;
            var rawScript = completions.Choices[0].Message.Content;
            await File.WriteAllTextAsync(ScriptPath, rawScript, cancellationToken);

            return ScriptPath;
        }
    }

    public class CreateScriptOptions
    {
        public string DeploymentName { get; set; }
        public float Temperature { get; set; }
        public int MaxTokens { get; set; }
        public string Topic { get; set; }
    }
}