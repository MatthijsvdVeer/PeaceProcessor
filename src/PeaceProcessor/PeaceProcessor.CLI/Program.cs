namespace PeaceProcessor.CLI;

using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PeaceProcessor commands");
        
        var topicOption = new Option<string>(
            aliases: ["--topic", "-t"],
            description: "The topic for which to create a script for.");
        var scriptCommand = new Command("script", "Create a script.");
        scriptCommand.AddOption(topicOption);
        
        scriptCommand.SetHandler((topic) => 
            { 
                Console.WriteLine($"Creating script for topic: {topic}"); 
            },
            topicOption);

        rootCommand.AddCommand(scriptCommand);
        return await rootCommand.InvokeAsync(args);
    }
}