namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Orchestrator;
    using Microsoft.CognitiveServices.Speech;
    using NAudio.Wave;
    using Azure.Storage.Blobs;

    internal sealed class CreateNarrationActivity
    {
        private readonly BlobContainerClient blobContainerClient;
        private readonly string key;

        private readonly string region;

        public CreateNarrationActivity(IOptions<SpeechConfiguration> config, BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
            this.key = config.Value.Key!;
            this.region = config.Value.Region!;
        }

        [Function(nameof(CreateNarrationActivity))]
        public async Task<string> Run([ActivityTrigger] CreateNarrationContext createNarrationContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(CreateNarrationActivity));
            var speechConfig = SpeechConfig.FromSubscription(this.key, this.region);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            var response = await this.blobContainerClient.GetBlobClient(createNarrationContext.ScriptPath).DownloadContentAsync();
            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await speechSynthesizer.SpeakSsmlAsync(response.Value.Content.ToString());

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                logger.LogError("Speech synthesis was cancelled with error code: {code}. Details: {details}", cancellation.ErrorCode, cancellation.ErrorDetails);
                throw new InvalidOperationException(cancellation.ErrorDetails);
            }

            // Convert to stereo.
            await using var waveFileReader = new WaveFileReader(new MemoryStream(result.AudioData));
            var stereo = new MonoToStereoProvider16(waveFileReader);
            var outputStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(outputStream, stereo);

            // Upload to blob storage.
            string blobPath = $"{createNarrationContext.Timestamp}/narration.wav";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);
            outputStream.Position = 0;
            await blobClient.UploadAsync(outputStream, true);
            
            return blobPath;
        }
    }
}