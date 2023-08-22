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

        private const string AudioPath = "audio.mp3";
        private const string ImagePath = "image.png";
        private const string OutputPath = "out.mp4";

        public CreateVideoActivity(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }

        [Function(nameof(CreateVideoActivity))]
        public async Task<string> Run([ActivityTrigger] CreateVideoContext createVideoContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateVideoActivity));
            logger.LogInformation("Creating video for audio {audio} and image {image}", createVideoContext.AudioPath,
                createVideoContext.ImagePath);

            var audioBlobClient = this.blobContainerClient.GetBlobClient(createVideoContext.AudioPath);
            await audioBlobClient.DownloadToAsync(AudioPath);
            logger.LogInformation("Downloaded {audioPath}", createVideoContext.AudioPath);

            var imageBlobClient = this.blobContainerClient.GetBlobClient(createVideoContext.ImagePath);
            await imageBlobClient.DownloadToAsync(ImagePath);
            logger.LogInformation("Downloaded {imagePath}", createVideoContext.ImagePath);

            var audioMediaInfo = await FFmpeg.GetMediaInfo(AudioPath);
            var audioStream = audioMediaInfo.AudioStreams.FirstOrDefault();

            var imageMediaInfo = await FFmpeg.GetMediaInfo(ImagePath);
            var videoStream = imageMediaInfo.VideoStreams.FirstOrDefault();

            var result = await FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .AddStream(audioStream)
                .SetOutput(OutputPath)
                .AddParameter("-loop 1", ParameterPosition.PreInput)
                .AddParameter("-tune stillimage", ParameterPosition.PostInput)
                .AddParameter("-shortest", ParameterPosition.PostInput)
                .AddParameter("-b:a 192k", ParameterPosition.PostInput)
                .Start();

            var blobPath = $"{createVideoContext.Timestamp}/output.mp4";
            var outputBlobClient = this.blobContainerClient.GetBlobClient(blobPath);
            await outputBlobClient.UploadAsync(File.OpenRead(OutputPath));
            return blobPath;
        }
    }
}