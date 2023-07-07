namespace PeaceProcessor.Functions
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NAudio.Wave;

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
        [BlobOutput("meditation/narration/{name}.wav", Connection = "storage-account")]
        public async Task<byte[]> Run([BlobTrigger("meditation/scripts/{name}.xml", Connection = "storage-account")] string myBlob, string name)
        {
            this.logger.LogInformation("{function} triggered for blob: {name}", nameof(GenerateNarrationFunction), name);

            var speechConfig = SpeechConfig.FromSubscription(this.key, this.region);

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await speechSynthesizer.SpeakSsmlAsync(myBlob);

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                this.logger.LogError(cancellation.ErrorDetails);
                throw new InvalidOperationException(cancellation.ErrorDetails);
            }

            // Convert to stereo.
            await using var waveFileReader = new WaveFileReader(new MemoryStream(result.AudioData));
            var stereo = new MonoToStereoProvider16(waveFileReader);
            var outputStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(outputStream, stereo);
            return outputStream.ToArray();
        }
    }
}
