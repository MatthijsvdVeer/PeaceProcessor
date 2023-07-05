using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, collection) =>
    {
        // collection.Configure<SpeechConfiguration>()
        var list = context.Configuration.GetChildren().OrderBy(section => section.Key).Select(section => section.Key).ToList();
        collection.Configure<SpeechConfiguration>(configuration =>
        {
            configuration.Key = context.Configuration["cog_speech_key"].ToString();
            configuration.Region = context.Configuration["cog_speech_region"].ToString();

        });
    })
    .Build();

host.Run();

public class SpeechConfiguration
{
    public string Key { get; set; }

    public string Region { get; set; }
}
