namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Queues;

    internal sealed class GetTopicActivity
    {
        private readonly QueueClient queueClient;
        public GetTopicActivity(QueueClient queueClient)
        {
            this.queueClient = queueClient;
        }

        [Function(nameof(GetTopicActivity))]
        public async Task<string> Run([ActivityTrigger] CreateScriptContext createScriptContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(GetTopicActivity));

            var responseMessage = await this.queueClient.ReceiveMessageAsync();
            if (responseMessage.Value != null)
            {
                return responseMessage.Value.Body.ToString();
            }

            logger.LogWarning("No topic found. Will not generate script.");
            return string.Empty;
        }
    }
}