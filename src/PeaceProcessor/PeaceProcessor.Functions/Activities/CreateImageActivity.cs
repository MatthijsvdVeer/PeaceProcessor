﻿namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.AI.OpenAI;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    internal sealed class CreateImageActivity
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly OpenAIClient openAiClient;

        public CreateImageActivity(OpenAiClientFactory openAiClientFactory, BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
            this.openAiClient = openAiClientFactory.Create(OpenAiKind.Image);
        }

        [Function(nameof(CreateImageActivity))]
        public async Task<string> Run([ActivityTrigger] CreateImageContext createImageContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateScriptActivity));
            logger.LogInformation("Creating image for prompt: {prompt}", createImageContext.ImagePrompt);

            var response = await this.openAiClient.GetImageGenerationsAsync(new ImageGenerationOptions(createImageContext.ImagePrompt));
            Stream stream = await new HttpClient().GetStreamAsync(response.Value.Data[0].Url);
            var blobPath = $"{createImageContext.StoragePath}/image.png";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);

            // Store the prompt in the blob metadata.
            Dictionary<string, string> metadata = new()
            {
                { "prompt", StringUtility.FormatForMetadata(createImageContext.ImagePrompt) }
            };

            var blobUploadOptions = new BlobUploadOptions
            {
                Metadata = metadata
            };

            await blobClient.UploadAsync(stream, blobUploadOptions);
            return blobPath;
        }
    }
}