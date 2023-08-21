namespace PeaceProcessor.Functions
{
    using Azure.Storage.Queues;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.DurableTask.Client;
    using Microsoft.Extensions.Logging;
    using Orchestrator;

    public class CreateScheduledMeditationFunction
    {
        private readonly QueueClient queueClient;

        public CreateScheduledMeditationFunction(QueueClient queueClient)
        {
            this.queueClient = queueClient;
        }

        [Function("CreateScheduledMeditationFunction")]
        public async Task Run(
            [TimerTrigger("%schedule%")] TimerInfo myTimer,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(CreateScheduledMeditationFunction));
            var responseMessage = await this.queueClient.ReceiveMessageAsync();
            if (responseMessage.Value == null)
            {
                logger.LogWarning("No topic found. Will not generate script.");
                return;
            }

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(CreateMeditationOrchestrator), responseMessage.Value.MessageText);
            logger.LogInformation("Created new orchestration with instance ID = {instanceId} for topic {topic}", instanceId, responseMessage.Value.MessageText);

            await this.queueClient.DeleteMessageAsync(responseMessage.Value.MessageId, responseMessage.Value.PopReceipt);
        }
    }
}