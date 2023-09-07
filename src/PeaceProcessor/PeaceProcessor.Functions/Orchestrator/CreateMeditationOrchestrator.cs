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
            
            var storagePath = context.CurrentUtcDateTime.ToString("yyyy/MM/dd/HHmmss");

            var createScriptContext = new CreateScriptContext(topic, storagePath);
            var script = await context.CallActivityAsync<string>(nameof(CreateScriptActivity), createScriptContext);

            var createNarrationContext = new CreateNarrationContext(script, storagePath);
            var narrationPath = await context.CallActivityAsync<string>(nameof(CreateNarrationActivity), createNarrationContext);

            var createVideoMetadataContext = new CreateVideoMetadataContext(script, topic, storagePath);
            var createVideoMetadata = context.CallActivityAsync<CreateVideoMetadataResponse>(nameof(CreateVideoMetadataActivity), createVideoMetadataContext);

            var addBackgroundContext = new AddBackgroundContext(narrationPath, storagePath);
            var addBackgroundMusic = context.CallActivityAsync<string>(nameof(AddBackgroundMusicActivity), addBackgroundContext);

            var createImagePrompt = context.CallActivityAsync<string>(nameof(CreateImagePromptActivity), createScriptContext);
            await Task.WhenAll(createImagePrompt, addBackgroundMusic, createVideoMetadata);

            var createImageContext = new CreateImageContext(createImagePrompt.Result, storagePath);
            var imagePath = await context.CallActivityAsync<string>(nameof(CreateImageActivity), createImageContext);

            var options = TaskOptions.FromRetryPolicy(new RetryPolicy(
                maxNumberOfAttempts: 1,
                firstRetryInterval: TimeSpan.FromSeconds(5)));
            var createVideoContext = new CreateVideoContext(addBackgroundMusic.Result, imagePath, storagePath);
            var videoPath = await context.CallActivityAsync<string>(nameof(CreateVideoActivity), createVideoContext, options);

            var uploadVideoContext = new UploadVideoContext(createVideoMetadata.Result.VideoTitle, createVideoMetadata.Result.VideoDescription, videoPath);
            var videoUrl = await context.CallActivityAsync<string>(nameof(UploadVideoActivity), uploadVideoContext);

            return videoUrl;
        }
    }
}
