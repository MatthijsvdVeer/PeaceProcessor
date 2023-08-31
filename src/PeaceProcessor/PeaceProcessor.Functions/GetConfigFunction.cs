namespace PeaceProcessor.Functions
{
    using System.Net;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.DurableTask.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class GetConfigFunction
    {
        private readonly IConfiguration configuration;

        public GetConfigFunction(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [Function("GetConfigFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(GetConfigFunction));
            var configKey = req.Query["key"];

            var configValue = this.configuration[configKey];
            HttpResponseData httpResponseData = req.CreateResponse(HttpStatusCode.OK);
            await httpResponseData.WriteStringAsync(configValue);
            return httpResponseData;
        }
    }
}
