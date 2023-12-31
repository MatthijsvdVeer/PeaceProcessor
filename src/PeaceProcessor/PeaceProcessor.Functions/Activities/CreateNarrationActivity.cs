﻿namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.CognitiveServices.Speech;
    using NAudio.Wave;
    using Azure.Storage.Blobs;
    using Azure.AI.OpenAI;
    using Azure.Storage.Blobs.Models;
    using System.Text;

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
            
            // Set the audio config to null. otherwise it will try to use the default audio device.
            // Pretty sure Azure data centers don't have a default audio device.
            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);
            var result = await speechSynthesizer.SpeakSsmlAsync(createNarrationContext.Script);

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
            var blobPath = $"{createNarrationContext.StoragePath}/narration.wav";
            var blobClient = this.blobContainerClient.GetBlobClient(blobPath);
            outputStream.Position = 0;

            // Create metadata for the blob.
            Dictionary<string, string> metadata = new()
            {
                { "duration", StringUtility.FormatForMetadata(result.AudioDuration.ToString()) }
            };

            var blobUploadOptions = new BlobUploadOptions
            {
                Metadata = metadata
            };

            await blobClient.UploadAsync(outputStream, blobUploadOptions);
            
            return blobPath;
        }
    }
}