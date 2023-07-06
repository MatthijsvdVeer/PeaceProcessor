using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PeaceProcessor.Functions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, collection) =>
    {
        collection.Configure<SpeechConfiguration>(configuration =>
            {
                configuration.Key = context.Configuration["cog_speech_key"].ToString();
                configuration.Region = context.Configuration["cog_speech_region"].ToString();

            })
            .AddScoped(_ => new BlobContainerClient(context.Configuration["blob-connection"], "meditation"))
            .AddScoped(_ => new QueueClient(context.Configuration["blob-connection"], "topics"));
    })
    .Build();

host.Run();