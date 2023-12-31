﻿namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Blobs;
    using Xabe.FFmpeg;

    internal sealed class CreateVideoActivity
    {
        private readonly BlobContainerClient blobContainerClient;

        private const string AudioFileName = "audio.mp3";
        private const string ImageFileName = "image.png";
        private const string OutputFileName = "out.mp4";

        public CreateVideoActivity(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }

        [Function(nameof(CreateVideoActivity))]
        public async Task<string> Run([ActivityTrigger] CreateVideoContext createVideoContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateVideoActivity));

            // Creating a unique output folder in case of activity retries.
            var uniqueId = Guid.NewGuid().ToString();
            var baseDir = $"../{uniqueId}";
            var audioPath = $"../{baseDir}/{AudioFileName}";
            var imagePath = $"../{baseDir}/{ImageFileName}";
            var outputPath = $"../{baseDir}/{OutputFileName}";

            try
            {
                Directory.CreateDirectory($"../{baseDir}");

#if !DEBUG
                FFmpeg.SetExecutablesPath("./");
#endif

                logger.LogInformation("Creating video for audio {audio} and image {image}",
                    createVideoContext.AudioPath,
                    createVideoContext.ImagePath);

                var audioBlobClient = this.blobContainerClient.GetBlobClient(createVideoContext.AudioPath);
                await audioBlobClient.DownloadToAsync(audioPath);
                logger.LogInformation("Downloaded {audioPath}", createVideoContext.AudioPath);

                var imageBlobClient = this.blobContainerClient.GetBlobClient(createVideoContext.ImagePath);
                await imageBlobClient.DownloadToAsync(imagePath);
                logger.LogInformation("Downloaded {imagePath}", createVideoContext.ImagePath);

                var audioMediaInfo = await FFmpeg.GetMediaInfo(audioPath);
                var audioStream = audioMediaInfo.AudioStreams.FirstOrDefault();

                var imageMediaInfo = await FFmpeg.GetMediaInfo(imagePath);
                var videoStream = imageMediaInfo.VideoStreams.FirstOrDefault();

                logger.LogInformation("Start FFmpeg for {output}", outputPath);
                try
                {
                    _ = await FFmpeg.Conversions.New()
                        .AddStream(videoStream)
                        .AddStream(audioStream)
                        .SetOutput(outputPath)
                        .AddParameter("-loop 1", ParameterPosition.PreInput)
                        .AddParameter("-tune stillimage", ParameterPosition.PostInput)
                        .AddParameter("-shortest", ParameterPosition.PostInput)
                        .AddParameter("-b:a 192k", ParameterPosition.PostInput)
                        .Start();
                    logger.LogInformation("Finished FFmpeg for {output}", outputPath);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error during FFmpeg.");
                    throw;
                }

                var blobPath = $"{createVideoContext.StoragePath}/output.mp4";
                var outputBlobClient = this.blobContainerClient.GetBlobClient(blobPath);
                await using FileStream fileStream = File.OpenRead(outputPath);
                await outputBlobClient.UploadAsync(fileStream);
                return blobPath;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error during video creation.");
                throw;
            }
            finally
            {
                Directory.Delete($"../{baseDir}", true);
            }
        }
    }
}