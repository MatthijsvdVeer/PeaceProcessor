namespace PeaceProcessor.Functions.Activities
{
    using Microsoft.Azure.Functions.Worker;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using NAudio.Wave.SampleProviders;
    using NAudio.Wave;

    internal sealed class AddBackgroundMusicActivity
    {
        private const int SampleRate = 16000;
        private const int SecondsPauseAfterNarration = 10;
        private readonly BlobContainerClient blobContainerClient;

        public AddBackgroundMusicActivity(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }

        [Function(nameof(AddBackgroundMusicActivity))]
        public async Task<string> Run([ActivityTrigger] AddBackgroundContext addBackgroundContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(AddBackgroundMusicActivity));
            var musicPath = await this.GetRandomMusicPath();
            logger.LogInformation("Picked music file {name}", musicPath);
            await using var musicStream = await this.GetStreamForBlobAsync(musicPath);
            
            // Read narration as a WAV file.
            logger.LogInformation("Reading narration: {path}", addBackgroundContext.NarrationPath);
            await using var narrationStream = await this.GetStreamForBlobAsync(addBackgroundContext.NarrationPath);
            await using var waveFileReader = new WaveFileReader(narrationStream);

            // Read the background music as an MP3.
            await using var mp3FileReader = new Mp3FileReader(musicStream);

            // Convert it to a WAV file with the same sample rate as the narration.
            var outFormat = new WaveFormat(SampleRate, mp3FileReader.WaveFormat.Channels);
            using var resampler = new MediaFoundationResampler(mp3FileReader, outFormat);

            // Mix the two audio streams together.
            var mixer = new MixingSampleProvider(
                new[] {waveFileReader.ToSampleProvider(), resampler.ToSampleProvider()});
            TimeSpan takeDuration = waveFileReader.TotalTime.Add(TimeSpan.FromSeconds(SecondsPauseAfterNarration));
            var sampleProvider = mixer.Take(takeDuration);

            // Return the mixed audio.
            MemoryStream outStream = new();
            WaveFileWriter.WriteWavFileToStream(outStream, sampleProvider.ToWaveProvider());
            outStream.Position = 0;

            var blobPath = $"{addBackgroundContext.StoragePath}/complete.wav";
            var completeBlob = this.blobContainerClient.GetBlobClient(blobPath);
            await completeBlob.UploadAsync(outStream, true);
            return blobPath;
        }

        private async Task<string> GetRandomMusicPath()
        {
            var blobs = this.blobContainerClient.GetBlobsAsync(BlobTraits.All, BlobStates.None, "music/");
            var blobList = new List<BlobItem>();
            await foreach (var blob in blobs)
            {
                blobList.Add(blob);
            }

            // Select random music file.
            var random = new Random();
            var randomMusic = blobList[random.Next(blobList.Count)];
            return randomMusic.Name;
        }

        private async Task<ContinuousStream> GetStreamForBlobAsync(string path)
        {
            var blobClient = this.blobContainerClient.GetBlobClient(path);
            return new ContinuousStream(await blobClient.OpenReadAsync());
        }
    }
}