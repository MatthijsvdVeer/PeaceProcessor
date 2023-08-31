using Azure.Identity;
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
            .AddScoped(_ => new BlobContainerClient(new Uri($"{context.Configuration["blob_connection"]}/meditation"),
                new DefaultAzureCredential()))
            .AddScoped(_ => new QueueClient(new Uri($"{context.Configuration["queue_connection"]}/topics"),
                new DefaultAzureCredential()))
            .AddTransient(_ => new OpenAiClientFactory(context.Configuration));
    })
    .Build();

host.Run();