namespace PeaceProcessor.Functions
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    internal sealed class AddBackgroundMusicFunction
    {
        private readonly ILogger logger;

        public AddBackgroundMusicFunction(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<AddBackgroundMusicFunction>();
        }

        [Function("AddBackgroundMusic")]
        [BlobOutput("meditation/complete/{name}.wav", Connection = "blob-connection")]
        public async Task<byte[]> Run(
            [BlobTrigger("meditation/narration/{name}.wav", Connection = "blob-connection")]
            byte[] myBlob, string name,
            [BlobInput("meditation/music/birds-forest.mp3", Connection = "blob-connection")]
            byte[] music)
        {
            this.logger.LogInformation("{function} triggered for blob: {name}", nameof(AddBackgroundMusicFunction),
                name);

            // Read narration as a WAV file.
            var narrationStream = new MemoryStream(myBlob);
            await using var waveFileReader = new WaveFileReader(narrationStream);

            // Read the background music as an MP3.
            var musicStream = new MemoryStream(music);
            await using var mp3FileReader = new Mp3FileReader(musicStream);

            // Convert it to a WAV file with the same sample rate as the narration.
            var outFormat = new WaveFormat(16000, mp3FileReader.WaveFormat.Channels);
            using var resampler = new MediaFoundationResampler(mp3FileReader, outFormat);

            // Mix the two audio streams together.
            var mixer = new MixingSampleProvider(
                new[] {waveFileReader.ToSampleProvider(), resampler.ToSampleProvider()});
            var sampleProvider = mixer.Take(waveFileReader.TotalTime.Add(TimeSpan.FromSeconds(10)));

            // Return the mixed audio.
            MemoryStream outStream = new();
            WaveFileWriter.WriteWavFileToStream(outStream, sampleProvider.ToWaveProvider());
            return outStream.ToArray();
        }
    }
}