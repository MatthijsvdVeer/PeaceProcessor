namespace PeaceProcessor.Functions
{
    using System.Net;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using Xabe.FFmpeg.Downloader;

    public sealed class DownloadFfmpegFunction
    {
        [Function("DownloadFfmpegFunction")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext functionContext)
        {
            var logger = functionContext.GetLogger<DownloadFfmpegFunction>();

            logger.LogInformation("Downloading Ffpmeg.");
            FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            logger.LogInformation("Finished downloading Ffpmeg.");

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
