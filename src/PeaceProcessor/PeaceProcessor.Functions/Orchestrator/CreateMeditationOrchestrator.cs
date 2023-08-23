﻿namespace PeaceProcessor.Functions.Orchestrator
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
            var addBackgroundMusic = context.CallActivityAsync<string>(nameof(AddBackgroundMusicActivity), addBackgroundContext);

            var createImagePrompt = context.CallActivityAsync<string>(nameof(CreateImagePromptActivity), createScriptContext);
            await Task.WhenAll(createImagePrompt, addBackgroundMusic);

            var createImageContext = new CreateImageContext(createImagePrompt.Result, timestamp);
            var imagePath = await context.CallActivityAsync<string>(nameof(CreateImageActivity), createImageContext);

            var createVideoContext = new CreateVideoContext(addBackgroundMusic.Result, imagePath, timestamp);
            var videoPath = await context.CallActivityAsync<string>(nameof(CreateVideoActivity), createVideoContext);

            return videoPath;
        }
    }
}
