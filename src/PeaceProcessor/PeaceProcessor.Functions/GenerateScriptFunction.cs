namespace PeaceProcessor.Functions
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Blobs;
    using System.Globalization;
    using System.Text;
    using Azure.Storage.Queues;

    internal sealed class GenerateScriptFunction
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly QueueClient queueClient;
        private readonly ILogger logger;

        public GenerateScriptFunction(BlobContainerClient blobContainerClient, QueueClient queueClient, ILoggerFactory loggerFactory)
        {
            this.blobContainerClient = blobContainerClient;
            this.queueClient = queueClient;
            this.logger = loggerFactory.CreateLogger<GenerateScriptFunction>(); 
        }

        [Function("GenerateScript")]
        public async Task Run([TimerTrigger("%schedule%")] MyInfo myTimer)
        {
            this.logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var responseMessage = await this.queueClient.ReceiveMessageAsync();
            if (responseMessage.Value == null)
            {
                this.logger.LogWarning("No topic found. Will not generate script.");
                return;
            }

            var format = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var blobClient = this.blobContainerClient.GetBlobClient($"scripts/{format}.xml");
            const string script = @"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-US'>
  <voice name='en-US-JennyNeural'>
    <mstts:express-as style=""hopeful"">
      <p>
        <s>As we come to the end of this meditation, take one last deep breath, inhaling deeply and exhaling slowly. Gently wiggle your fingers and toes. When you’re ready, open your eyes. Thank you for your presence during this session.</s>
      </p>
    </mstts:express-as>
  </voice>
</speak>";
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(script)));
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
