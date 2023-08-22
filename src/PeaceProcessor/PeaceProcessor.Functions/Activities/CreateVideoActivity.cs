namespace PeaceProcessor.Functions.Activities
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
            var audioPath = $"../{createVideoContext.Timestamp}/{AudioFileName}";
            var imagePath = $"../{createVideoContext.Timestamp}/{ImageFileName}";
            var outputPath = $"../{createVideoContext.Timestamp}/{OutputFileName}";

            try
            {
                Directory.CreateDirectory($"../{createVideoContext.Timestamp}");

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

                _ = await FFmpeg.Conversions.New()
                    .AddStream(videoStream)
                    .AddStream(audioStream)
                    .SetOutput(outputPath)
                    .AddParameter("-loop 1", ParameterPosition.PreInput)
                    .AddParameter("-tune stillimage", ParameterPosition.PostInput)
                    .AddParameter("-shortest", ParameterPosition.PostInput)
                    .AddParameter("-b:a 192k", ParameterPosition.PostInput)
                    .Start();

                var blobPath = $"{createVideoContext.Timestamp}/output.mp4";
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
                Directory.Delete($"../{createVideoContext.Timestamp}", true);
            }
        }
    }
}