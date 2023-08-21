namespace PeaceProcessor.Functions.Orchestrator
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.DurableTask;
    using System.Threading.Tasks;
    using Activities;

    internal sealed class CreateMeditationOrchestrator
    {
        [Function("CreateMeditationOrchestrator")]
        public static async Task<string> Run([OrchestrationTrigger]TaskOrchestrationContext context)
        {
            var topic = context.GetInput<string>() ?? throw new InvalidOperationException("Topic is null");
            var timestamp = context.CurrentUtcDateTime.ToString("yyyyMMddHHmmss");

            var createScriptContext = new CreateScriptContext(topic, timestamp);
            var scriptPath = await context.CallActivityAsync<string>(nameof(CreateScriptActivity), createScriptContext);

            var createNarrationContext = new CreateNarrationContext(scriptPath, timestamp);
            var narrationPath = await context.CallActivityAsync<string>(nameof(CreateNarrationActivity), createNarrationContext);

            var addBackgroundContext = new AddBackgroundContext(narrationPath, timestamp);
            var completePath = await context.CallActivityAsync<string>(nameof(AddBackgroundMusicActivity), addBackgroundContext);
            return completePath;
        }
    }
}
