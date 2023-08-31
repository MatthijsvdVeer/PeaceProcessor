namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.AI.OpenAI;
    using Azure.Storage.Blobs;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.YouTube.v3;
    using Google.Apis.Services;
    using Google.Apis.YouTube.v3.Data;
    using Google.Apis.Upload;
    using Microsoft.Extensions.Configuration;

    internal sealed class UploadVideoActivity
    {
        private const string GenericVideoCategoryId = "22";
        private readonly BlobContainerClient blobContainerClient;
        private readonly string userAccount;
        private readonly string playlist;
        private readonly string principal;

        public UploadVideoActivity(OpenAiClientFactory aiClientFactory, BlobContainerClient blobContainerClient,
            IConfiguration configuration)
        {
            this.blobContainerClient = blobContainerClient;
            this.userAccount = configuration["youtube_user_account"];
            this.playlist = configuration["youtube_playlist"];
            this.principal = configuration["youtube_principal"];
        }

        [Function(nameof(UploadVideoActivity))]
        public async Task<string> Run(
            [ActivityTrigger] UploadVideoContext uploadVideoContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(UploadVideoActivity));
            logger.LogInformation("Uploading video {topic}", uploadVideoContext.VideoPath);

            var credential = this.GetGoogleCredential(logger);

            // Create the YouTube service
            var youTubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "PeaceProcessor"
            });

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = uploadVideoContext.Title,
                    Description = uploadVideoContext.Description,
                    Tags = Array.Empty<string>(),
                    CategoryId = GenericVideoCategoryId
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = "private"
                }
            };


            var blobClient = this.blobContainerClient.GetBlobClient(uploadVideoContext.VideoPath);
            await using var fileStream = await blobClient.OpenReadAsync();
            var videosInsertRequest = youTubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            videosInsertRequest.ProgressChanged += progress => ProgressChanged(progress, logger);

            Video? videoResult = null;
            videosInsertRequest.ResponseReceived += v =>
            {
                logger.LogInformation("Upload completed.");
                videoResult = v;
            };

            IUploadProgress progress = await videosInsertRequest.UploadAsync();
            if (progress.Status != UploadStatus.Completed || videoResult == null)
            {
                throw new InvalidOperationException("Upload failed.");
            }

            await this.AddVideoToPlaylistAsync(videoResult.Id, youTubeService);

            return $"https://www.youtube.com/watch?v={videoResult.Id}";
        }

        private GoogleCredential GetGoogleCredential(ILogger logger)
        {
            // Load the service account credentials
            try
            {
                GoogleCredential credential = GoogleCredential.FromJson(this.principal)
                    .CreateScoped(YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.Youtube);

                credential = credential.CreateWithUser(this.userAccount);
                return credential;
            }

            catch (Exception exception)
            {
                logger.LogError(exception, "Error while parsing principal.");
                throw;
            }
        }

        private static void ProgressChanged(IUploadProgress progress, ILogger logger)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    logger.LogInformation("Uploading file... {BytesSent} bytes sent.", progress.BytesSent);
                    break;
                case UploadStatus.Failed:
                    logger.LogError("An error prevented the upload from completing.\n{exception}", progress.Exception);
                    break;
                case UploadStatus.NotStarted:
                    break;
                case UploadStatus.Starting:
                    break;
                case UploadStatus.Completed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(progress));
            }
        }

        private async Task AddVideoToPlaylistAsync(string videoId, YouTubeService youTubeService)
        {
            var playlistItem = new PlaylistItem
            {
                Snippet = new PlaylistItemSnippet
                {
                    PlaylistId = this.playlist,
                    ResourceId = new ResourceId
                    {
                        Kind = "youtube#video",
                        VideoId = videoId
                    }
                }
            };

            var insertRequest = youTubeService.PlaylistItems.Insert(playlistItem, "snippet");
            await insertRequest.ExecuteAsync();
        }
    }
}