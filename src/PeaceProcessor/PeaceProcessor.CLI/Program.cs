namespace PeaceProcessor.CLI;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using Core.UseCases;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        var a = BuildCommandLine()
                .UseHost(_ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddSingleton<CreateScript>();
                        });
                    })
            ;
        var foo = a.Build();
        // foo.Parse(args);
        await foo.InvokeAsync("create-script --topic 'meditation'");
        var parseResult = foo.Parse("create-script --topic 'meditation'");
        var exitCode = await parseResult.InvokeAsync();
        Environment.Exit(exitCode);
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        var root = new RootCommand(@"Stuff");

        var topicOption = new Option<string>(
            aliases: ["--topic", "-t"],
            description: "The topic for which to create a script for.")
        {
            IsRequired = true
        };
        var createScriptCommand = new Command("create-script", "Create a new meditation script.");
        createScriptCommand.AddOption(topicOption);
        createScriptCommand.SetHandler(() => CommandHandler.Create<IHost>((host) =>
            host.Services.GetRequiredService<CreateScript>().ExecuteAsync()));
        
        createScriptCommand.SetHandler((topic) => 
            { 
                host.Services.GetRequiredService<CreateScript>().ExecuteAsync(options.Topic)))
                Console.WriteLine($"Creating script for topic: {topic}"); 
            },
            topicOption);
        
        root.AddCommand(createScriptCommand);
        return new CommandLineBuilder(root);
    }

    private static void Run(CreateScriptOptions options, IHost host)
    {
        var serviceProvider = host.Services;
        var greeter = serviceProvider.GetRequiredService<CreateScript>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(Program));

        var name = options.Topic;
        logger.LogInformation("Greeting was requested for: {name}", name);
        greeter.ExecuteAsync(name);
    }
}