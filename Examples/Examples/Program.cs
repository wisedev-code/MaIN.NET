using Examples;
using Examples.Agents;
using Examples.Agents.Flows;
using MaIN.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


var Banner = @"
███╗   ███╗ █████╗ ██╗███╗   ██╗    ███████╗██╗  ██╗ █████╗ ███╗   ███╗██████╗ ██╗     ███████╗███████╗
████╗ ████║██╔══██╗██║████╗  ██║    ██╔════╝╚██╗██╔╝██╔══██╗████╗ ████║██╔══██╗██║     ██╔════╝██╔════╝
██╔████╔██║███████║██║██╔██╗ ██║    █████╗   ╚███╔╝ ███████║██╔████╔██║██████╔╝██║     █████╗  ███████╗
██║╚██╔╝██║██╔══██║██║██║╚██╗██║    ██╔══╝   ██╔██╗ ██╔══██║██║╚██╔╝██║██╔═══╝ ██║     ██╔══╝  ╚════██║
██║ ╚═╝ ██║██║  ██║██║██║ ╚████║    ███████╗██╔╝ ██╗██║  ██║██║ ╚═╝ ██║██║     ███████╗███████╗███████║
╚═╝     ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝    ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     ╚═╝╚═╝     ╚══════╝╚══════╝╚══════╝
                                                                                                
╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗
                                    Interactive Example Runner v1.0                                     ";


Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(Banner);
Console.ResetColor();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMaIN(configuration);

RegisterExamples(services);

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseMaIN();

await RunSelectedExample(serviceProvider);


static void RegisterExamples(IServiceCollection services)
{
    services.AddTransient<ExampleRegistry>();
    services.AddTransient<ChatExample>();
    services.AddTransient<ChatWithFilesExample>();
    services.AddTransient<ChatWithFilesFromStreamExample>();
    services.AddTransient<ChatWithVisionExample>();
    services.AddTransient<ChatWithImageGenExample>();
    services.AddTransient<ChatFromExistingExample>();
    services.AddTransient<ChatWithReasoningExample>();
    services.AddTransient<AgentExample>();
    services.AddTransient<AgentWithRedirectExample>();
    services.AddTransient<MultiBackendAgentWithRedirectExample>();
    services.AddTransient<AgentWithRedirectImageExample>();
    services.AddTransient<AgentWithBecomeExample>();
    services.AddTransient<AgentWithApiDataSourceExample>();
    services.AddTransient<AgentTalkingToEachOtherExample>();
    services.AddTransient<AgentsComposedAsFlowExample>();
    services.AddTransient<AgentsFlowLoadedExample>();
    services.AddTransient<ChatExampleOpenAi>();
    services.AddTransient<AgentWithWebDataSourceOpenAiExample>();
    services.AddTransient<ChatWithImageGenOpenAiExample>();
}

async Task RunSelectedExample(IServiceProvider serviceProvider)
{
    var registry = serviceProvider.GetRequiredService<ExampleRegistry>();
    var examples = registry.GetAvailableExamples();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n┌─────────────────────────────────────────────┐");
    Console.WriteLine("│             Available Examples              │");
    Console.WriteLine("└─────────────────────────────────────────────┘");
    Console.ResetColor();

    for (int i = 0; i < examples.Count; i++)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\n [{i + 1}] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(examples[i].Name);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n┌─────────────────────────────────────────────┐");
    Console.Write($"│ >> Select example (1-{examples.Count}): ");
    Console.CursorLeft = 45;
    Console.WriteLine("│");
    Console.WriteLine("└─────────────────────────────────────────────┘");
    Console.ForegroundColor = ConsoleColor.White;

    if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= examples.Count)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(Banner);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n>> Running: {examples[selection - 1].Name}");
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          Output Below                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var selectedExample = examples[selection - 1].Instance;
        await selectedExample.Start();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║  [X] Error: Invalid selection. Please try again.     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }
}


public class ExampleRegistry(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public List<(string Name, IExample Instance)> GetAvailableExamples()
    {
        return new List<(string, IExample)>
        {
            ("\u25a0 Basic Chat", _serviceProvider.GetRequiredService<ChatExample>()),
            ("\u25a0 Chat with Files", _serviceProvider.GetRequiredService<ChatWithFilesExample>()),
            ("\u25a0 Chat with Files from stream", _serviceProvider.GetRequiredService<ChatWithFilesFromStreamExample>()),
            ("\u25a0 Chat with Vision", _serviceProvider.GetRequiredService<ChatWithVisionExample>()),
            ("\u25a0 Chat with Image Generation", _serviceProvider.GetRequiredService<ChatWithImageGenExample>()),
            ("\u25a0 Chat from Existing", _serviceProvider.GetRequiredService<ChatFromExistingExample>()),
            ("\u25a0 Chat with reasoning", _serviceProvider.GetRequiredService<ChatWithReasoningExample>()),
            ("\u25a0 Basic Agent", _serviceProvider.GetRequiredService<AgentExample>()),
            ("\u25a0 Agent with Redirect", _serviceProvider.GetRequiredService<AgentWithRedirectExample>()),
            ("\u25a0 Agent with Redirect (Multi backends)", _serviceProvider.GetRequiredService<MultiBackendAgentWithRedirectExample>()),
            ("\u25a0 Agent with Redirect Image", _serviceProvider.GetRequiredService<AgentWithRedirectImageExample>()),
            ("\u25a0 Agent with Become", _serviceProvider.GetRequiredService<AgentWithBecomeExample>()),
            ("\u25a0 Agent with API Data Source", _serviceProvider.GetRequiredService<AgentWithApiDataSourceExample>()),
            ("\u25a0 Agents Talking to Each Other", _serviceProvider.GetRequiredService<AgentTalkingToEachOtherExample>()),
            ("\u25a0 Agents Composed as Flow", _serviceProvider.GetRequiredService<AgentsComposedAsFlowExample>()),
            ("\u25a0 Agents Flow Loaded", _serviceProvider.GetRequiredService<AgentsFlowLoadedExample>()),
            ("\u25a0 OpenAi Chat", _serviceProvider.GetRequiredService<ChatExampleOpenAi>()),
            ("\u25a0 OpenAi Chat with image", _serviceProvider.GetRequiredService<ChatWithImageGenOpenAiExample>()),
            ("\u25a0 OpenAi Agent with Web Data Source", _serviceProvider.GetRequiredService<AgentWithWebDataSourceOpenAiExample>())

        };
    }
}