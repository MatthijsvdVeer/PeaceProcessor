namespace PeaceProcessor.Functions
{
    using System.Net;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.DurableTask.Client;
    using Microsoft.Extensions.Logging;
    using Orchestrator;

    public class CreateMeditationFunction
    {
        [Function("CreateMeditationFunction")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateMeditationFunction));
            var requestBody = await req.ReadFromJsonAsync<RequestBody>();

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(CreateMeditationOrchestrator), requestBody.Topic);
            logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }

    internal sealed class RequestBody
    {
        public string Topic { get; set; }
    }
}
