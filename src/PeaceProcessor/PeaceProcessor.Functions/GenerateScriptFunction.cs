namespace PeaceProcessor.Functions
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Blobs;
    using System.Globalization;
    using System.Text;
    using Azure.Storage.Queues;
    using System.Net.Http.Json;
    using System.Text.Json;

    internal sealed class GenerateScriptFunction
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly QueueClient queueClient;
        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        public GenerateScriptFunction(BlobContainerClient blobContainerClient, QueueClient queueClient, IHttpClientFactory httpClient, ILoggerFactory loggerFactory)
        {
            this.blobContainerClient = blobContainerClient;
            this.queueClient = queueClient;
            this.httpClient = httpClient.CreateClient(nameof(GenerateScriptFunction));
            this.logger = loggerFactory.CreateLogger<GenerateScriptFunction>(); 
        }

        [Function("GenerateScript")]
        public async Task Run([TimerTrigger("%schedule%")] MyInfo myTimer)
        {
            this.logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            var responseMessage = await this.queueClient.ReceiveMessageAsync();
            if (responseMessage.Value == null)
            {
                this.logger.LogWarning("No topic found. Will not generate script.");
                return;
            }

            var prompt = await File.ReadAllTextAsync("prompt.txt");

            var request = new OpenAiRequest("gpt-4", 0.5);
            request.Messages.Add(new OpenAiMessage(OpenAiRole.System, prompt));
            request.Messages.Add(new OpenAiMessage(OpenAiRole.User, $"Today's topic will be {responseMessage.Value.Body}"));
            string content = @"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-US'>
  <voice name='en-US-JennyNeural'>
    <mstts:express-as style=""hopeful"">
      <p>";
            request.Messages.Add(new OpenAiMessage(OpenAiRole.Assistant, content));

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };
            options.Converters.Add(new OpenAiRoleEnumJsonConverter());
            var jsonContent = JsonContent.Create(request, options: options);
            var response = await this.httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);

            var format = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var blobClient = this.blobContainerClient.GetBlobClient($"scripts/{format}.xml");
            var openAiResponse = await response.Content.ReadFromJsonAsync<OpenAiResponse>(options);
            string messageContent = openAiResponse.Choices.Single().Message.Content;
            string xml = content + messageContent;

            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
            await this.queueClient.DeleteMessageAsync(responseMessage.Value.MessageId,
                responseMessage.Value.PopReceipt);
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
