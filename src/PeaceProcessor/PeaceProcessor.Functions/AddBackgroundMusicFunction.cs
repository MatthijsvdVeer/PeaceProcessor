namespace PeaceProcessor.Functions
{
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    internal sealed class AddBackgroundMusicFunction
    {
        private const int SampleRate = 16000;
        private const int SecondsPauseAfterNarration = 10;
        private readonly BlobContainerClient blobContainerClient;
        private readonly ILogger logger;

        public AddBackgroundMusicFunction(BlobContainerClient blobContainerClient, ILoggerFactory loggerFactory)
        {
            this.blobContainerClient = blobContainerClient;
            this.logger = loggerFactory.CreateLogger<AddBackgroundMusicFunction>();
        }

        [Function("AddBackgroundMusic")]
        [BlobOutput("meditation/complete/{name}.wav", Connection = "storage_account")]
        public async Task<byte[]> Run(
            [BlobTrigger("meditation/narration/{name}.wav", Connection = "storage_account")]
            byte[] myBlob, string name)
        {
            this.logger.LogInformation("{function} triggered for blob: {name}", nameof(AddBackgroundMusicFunction),
                name);

            var blobs = this.blobContainerClient.GetBlobsAsync(BlobTraits.All, BlobStates.None, "music/");
            var blobList = new List<BlobItem>();
            await foreach (var blob in blobs)
            {
                blobList.Add(blob);
            }

            // Select random music file.
            var random = new Random();
            var randomMusic = blobList[random.Next(blobList.Count)];
            var blobClient = this.blobContainerClient.GetBlobClient(randomMusic.Name);
            Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();
            using MemoryStream ms = new();
            await response.Value.Content.CopyToAsync(ms);

            // Reset the position to the start of the stream.
            ms.Position = 0;

            // Read narration as a WAV file.
            var narrationStream = new MemoryStream(myBlob);
            await using var waveFileReader = new WaveFileReader(narrationStream);

            // Read the background music as an MP3.
            await using var mp3FileReader = new Mp3FileReader(ms);

            // Convert it to a WAV file with the same sample rate as the narration.
            var outFormat = new WaveFormat(SampleRate, mp3FileReader.WaveFormat.Channels);
            using var resampler = new MediaFoundationResampler(mp3FileReader, outFormat);

            // Mix the two audio streams together.
            var mixer = new MixingSampleProvider(
                new[] {waveFileReader.ToSampleProvider(), resampler.ToSampleProvider()});
            var sampleProvider =
                mixer.Take(waveFileReader.TotalTime.Add(TimeSpan.FromSeconds(SecondsPauseAfterNarration)));

            // Return the mixed audio.
            MemoryStream outStream = new();
            WaveFileWriter.WriteWavFileToStream(outStream, sampleProvider.ToWaveProvider());
            return outStream.ToArray();
        }
    }
}