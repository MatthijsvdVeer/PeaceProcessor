using Microsoft.Azure.Functions.Worker;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PeaceProcessor.Functions
{
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;
    using System.Reflection.PortableExecutable;

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
            this.logger.LogInformation("{function} triggered for blob: {name}", nameof(AddBackgroundMusicFunction), name);
            await Task.Delay(1000);

            // Read the narration and convert to stereo.
            var stream = new MemoryStream(myBlob);
            await using var waveFileReader = new WaveFileReader(stream);
            var stereo = new MonoToStereoProvider16(waveFileReader);

            // Read the background music as an MP3.
            var stream2 = new MemoryStream(music);
            await using var mp3FileReader = new Mp3FileReader(stream2);

            // Convert it to a WAV file with the same sample rate as the narration.
            var outFormat = new WaveFormat(16000, mp3FileReader.WaveFormat.Channels);
            using (var resampler = new MediaFoundationResampler(mp3FileReader, outFormat))
            {
                WaveFileWriter.CreateWaveFile("music.wav", resampler);
            }

            await using var reader2 = new WaveFileReader("music.wav");
            
            // Mix the two audio streams together.
            var mixer = new MixingSampleProvider(new[]
                {stereo.ToSampleProvider(), reader2.ToSampleProvider()});
            var sampleProvider = mixer.Take(waveFileReader.TotalTime.Add(TimeSpan.FromSeconds(10)));
            WaveFileWriter.CreateWaveFile16("mixed.wav", sampleProvider);

            // Return the mixed audio.
            var bytes = await File.ReadAllBytesAsync("mixed.wav");
            this.logger.LogInformation("Returning blob data. {bytes} bytes.", bytes.Length);
            return bytes;
        }
    }
}