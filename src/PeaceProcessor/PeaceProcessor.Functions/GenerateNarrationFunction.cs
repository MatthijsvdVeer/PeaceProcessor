using Microsoft.Azure.Functions.Worker;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PeaceProcessor.Functions
{
    internal sealed class GenerateNarrationFunction
    {
        private readonly ILogger logger;

        private readonly string key;

        private readonly string region;

        public GenerateNarrationFunction(IOptions<SpeechConfiguration> config, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<GenerateNarrationFunction>();
            this.key = config.Value.Key!;
            this.region = config.Value.Region!;

        }

        [Function("GenerateNarration")]
        [BlobOutput("meditation/narration/{name}.wav", Connection = "blob-connection")]
        public async Task<byte[]> Run([BlobTrigger("meditation/scripts/{name}.xml", Connection = "blob-connection")] string myBlob, string name)
        {
            this.logger.LogInformation("{function} triggered for blob: {name}", nameof(GenerateNarrationFunction), name);

            var speechConfig = SpeechConfig.FromSubscription(this.key, this.region);
            using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
            var result = await speechSynthesizer.SpeakSsmlAsync(myBlob);

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                this.logger.LogError(cancellation.ErrorDetails);
                throw new InvalidOperationException(cancellation.ErrorDetails);
            }

            var audioData = result.AudioData;
            this.logger.LogInformation("Returning blob data. {bytes} bytes.", audioData.Length);
            return audioData;
        }
    }
}
