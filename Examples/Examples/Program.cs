using Examples;
using Examples.Agents;
using Examples.Agents.Flows;
using Examples.Agents.Skills;
using Examples.Chat;
using Examples.Mcp;
using MaIN.Core;
using MaIN.Domain.Entities.Skills;
using MaIN.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var Banner = @"
███╗   ███╗ █████╗ ██╗███╗   ██╗    ███████╗██╗  ██╗ █████╗ ███╗   ███╗██████╗ ██╗     ███████╗███████╗
████╗ ████║██╔══██╗██║████╗  ██║    ██╔════╝╚██╗██╔╝██╔══██╗████╗ ████║██╔══██╗██║     ██╔════╝██╔════╝
██╔████╔██║███████║██║██╔██╗ ██║    █████╗   ╚███╔╝ ███████║██╔████╔██║██████╔╝██║     █████╗  ███████╗
██║╚██╔╝██║██╔══██║██║██║╚██╗██║    ██╔══╝   ██╔██╗ ██╔══██║██║╚██╔╝██║██╔═══╝ ██║     ██╔══╝  ╚════██║
██║ ╚═╝ ██║██║  ██║██║██║ ╚████║    ███████╗██╔╝ ██╗██║  ██║██║ ╚═╝ ██║██║     ███████╗███████╗███████║
╚═╝     ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝    ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     ╚═╝╚═╝     ╚══════╝╚══════╝╚══════╝
                                                                                                
╔═════════════════════════════════════════════════════════════════════════════════════════════════════╗
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

services.AddSkillsFromDirectory("./skills");
services.AddSingleton<IAgentSkillProvider, CalculatorSkill>();

RegisterExamples(services);

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseMaIN();

await RunSelectedExample(serviceProvider);

static void RegisterExamples(IServiceCollection services)
{
    services.AddTransient<ExampleRegistry>();
    services.AddTransient<McpExample>();
    services.AddTransient<ChatExample>();
    services.AddTransient<ChatCustomGrammarExample>();
    services.AddTransient<ChatWithFilesExample>();
    services.AddTransient<ChatWithFilesFromStreamExample>();
    services.AddTransient<ChatWithVisionExample>();
    services.AddTransient<ChatWithImageGenExample>();
    services.AddTransient<ChatFromExistingExample>();
    services.AddTransient<ChatWithReasoningExample>();
    services.AddTransient<ChatExampleToolsSimple>();
    services.AddTransient<ChatExampleToolsSimpleLocalLLM>();
    services.AddTransient<AgentExampleTools>();
    services.AddTransient<AgentExample>();
    services.AddTransient<AgentConversationExample>();
    services.AddTransient<AgentWithRedirectExample>();
    services.AddTransient<MultiBackendAgentWithRedirectExample>();
    services.AddTransient<McpAgentsExample>();
    services.AddTransient<AgentWithRedirectImageExample>();
    services.AddTransient<AgentWithBecomeExample>();
    services.AddTransient<AgentWithApiDataSourceExample>();
    services.AddTransient<AgentTalkingToEachOtherExample>();
    services.AddTransient<AgentWithKnowledgeFileExample>();
    services.AddTransient<AgentWithKnowledgeWebExample>();
    services.AddTransient<AgentWithKnowledgeMcpExample>();
    services.AddTransient<AgentsComposedAsFlowExample>();
    services.AddTransient<AgentsFlowLoadedExample>();
    services.AddTransient<ChatExampleOpenAi>();
    services.AddTransient<AgentWithWebDataSourceOpenAiExample>();
    services.AddTransient<AgentWithSkillsExample>();
    services.AddTransient<AgentWithFileSkillExample>();
    services.AddTransient<AgentWithFolderSkillExample>();
    services.AddTransient<AgentWithCustomCodeSkillExample>();
    services.AddTransient<AgentWithAllSkillsExample>();
    services.AddTransient<AgentWithSkillLocalModelExample>();
    services.AddTransient<AgentWithMcpFileWriterSkillExample>();
    services.AddTransient<ChatWithImageGenOpenAiExample>();
    services.AddTransient<ChatExampleGemini>();
    services.AddTransient<ChatGrammarExampleGemini>();
    services.AddTransient<ChatWithImageGenGeminiExample>();
    services.AddTransient<ChatWithFilesExampleGemini>();
    services.AddTransient<ChatExampleVertex>();
    services.AddTransient<ChatWithReasoningDeepSeekExample>();
    services.AddTransient<ChatWithTextToSpeechExample>();
    services.AddTransient<ChatExampleGroqCloud>();
    services.AddTransient<ChatExampleAnthropic>();
    services.AddTransient<ChatExampleXai>();
    services.AddTransient<ChatExampleOllama>();
    services.AddTransient<ChatWithCustomModelIdExample>();
}

async Task RunSelectedExample(IServiceProvider serviceProvider)
{
    var registry = serviceProvider.GetRequiredService<ExampleRegistry>();
    var examples = registry.GetAvailableExamples();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n┌──────────────────────────────────────────────┐");
    Console.WriteLine("│             Available Examples              │");
    Console.WriteLine("└──────────────────────────────────────────────┘");
    Console.ResetColor();

    for (int i = 0; i < examples.Count; i++)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\n [{i + 1}] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(examples[i].Name);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n┌──────────────────────────────────────────────┐");
    Console.Write($"│ >> Select example (1-{examples.Count}): ");
    Console.CursorLeft = 45;
    Console.WriteLine("│");
    Console.WriteLine("└──────────────────────────────────────────────┘");
    Console.ForegroundColor = ConsoleColor.White;

    if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= examples.Count)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(Banner);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n>> Running: {examples[selection - 1].Name}");
        Console.WriteLine("╔═════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          Output Below                              ║");
        Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        var selectedExample = examples[selection - 1].Instance;
        try
        {
            await selectedExample.Start();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                               Error                                ║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine(ex.Message);
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n╔═════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  [X] Error: Invalid selection. Please try again.     ║");
        Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }
}

namespace Examples
{
    public class ExampleRegistry(IServiceProvider serviceProvider)
    {
        public List<(string Name, IExample Instance)> GetAvailableExamples()
        {
            return
            [
                ("■ Basic Chat", serviceProvider.GetRequiredService<ChatExample>()),
                ("■ Chat with Files", serviceProvider.GetRequiredService<ChatWithFilesExample>()),
                ("■ Chat with custom grammar", serviceProvider.GetRequiredService<ChatCustomGrammarExample>()),
                ("■ Chat with Files from stream", serviceProvider.GetRequiredService<ChatWithFilesFromStreamExample>()),
                ("■ Chat with Vision", serviceProvider.GetRequiredService<ChatWithVisionExample>()),
                ("■ Chat with Tools (simple)", serviceProvider.GetRequiredService<ChatExampleToolsSimple>()),
                ("■ Chat with Tools (simple Local LLM)", serviceProvider.GetRequiredService<ChatExampleToolsSimpleLocalLLM>()),
                ("■ Chat with Image Generation", serviceProvider.GetRequiredService<ChatWithImageGenExample>()),
                ("■ Chat from Existing", serviceProvider.GetRequiredService<ChatFromExistingExample>()),
                ("■ Chat with reasoning", serviceProvider.GetRequiredService<ChatWithReasoningExample>()),
                ("■ Basic Agent", serviceProvider.GetRequiredService<AgentExample>()),
                ("■ Conversation Agent", serviceProvider.GetRequiredService<AgentConversationExample>()),
                ("■ Agent with Redirect", serviceProvider.GetRequiredService<AgentWithRedirectExample>()),
                ("■ Agent with Redirect (Multi backends)", serviceProvider.GetRequiredService<MultiBackendAgentWithRedirectExample>()),
                ("■ Agent with Redirect Image", serviceProvider.GetRequiredService<AgentWithRedirectImageExample>()),
                ("■ Agent with Become", serviceProvider.GetRequiredService<AgentWithBecomeExample>()),
                ("■ Agent with Tools (advanced)", serviceProvider.GetRequiredService<AgentExampleTools>()),
                ("■ Agent with Knowledge", serviceProvider.GetRequiredService<AgentWithKnowledgeFileExample>()),
                ("■ Agent with Web Knowledge", serviceProvider.GetRequiredService<AgentWithKnowledgeWebExample>()),
                ("■ Agent with Mcp Knowledge", serviceProvider.GetRequiredService<AgentWithKnowledgeMcpExample>()),
                ("■ Agent with API Data Source", serviceProvider.GetRequiredService<AgentWithApiDataSourceExample>()),
                ("■ Agents Talking to Each Other", serviceProvider.GetRequiredService<AgentTalkingToEachOtherExample>()),
                ("■ Agents Composed as Flow", serviceProvider.GetRequiredService<AgentsComposedAsFlowExample>()),
                ("■ Agents Flow Loaded", serviceProvider.GetRequiredService<AgentsFlowLoadedExample>()),
                ("■ OpenAi Chat", serviceProvider.GetRequiredService<ChatExampleOpenAi>()),
                ("■ OpenAi Chat with image", serviceProvider.GetRequiredService<ChatWithImageGenOpenAiExample>()),
                ("■ OpenAi Agent with Web Data Source", serviceProvider.GetRequiredService<AgentWithWebDataSourceOpenAiExample>()),
                ("■ Agent with Skills (file-based .md)", serviceProvider.GetRequiredService<AgentWithFileSkillExample>()),
                ("■ Agent with Skills (folder-based SKILL.md)", serviceProvider.GetRequiredService<AgentWithFolderSkillExample>()),
                ("■ Agent with Skills (custom C# skill)", serviceProvider.GetRequiredService<AgentWithCustomCodeSkillExample>()),
                ("■ Agent with Skills (.WithAllSkills)", serviceProvider.GetRequiredService<AgentWithAllSkillsExample>()),
                ("■ Agent with Skills (Local Model)", serviceProvider.GetRequiredService<AgentWithSkillLocalModelExample>()),
                ("■ Agent with Skills (MCP file writer)", serviceProvider.GetRequiredService<AgentWithMcpFileWriterSkillExample>()),
                ("■ Gemini Chat", serviceProvider.GetRequiredService<ChatExampleGemini>()),
                ("■ Gemini Chat with grammar", serviceProvider.GetRequiredService<ChatGrammarExampleGemini>()),
                ("■ Gemini Chat with image", serviceProvider.GetRequiredService<ChatWithImageGenGeminiExample>()),
                ("■ Gemini Chat with files", serviceProvider.GetRequiredService<ChatWithFilesExampleGemini>()),
                ("■ Vertex Chat", serviceProvider.GetRequiredService<ChatExampleVertex>()),
                ("■ DeepSeek Chat with reasoning", serviceProvider.GetRequiredService<ChatWithReasoningDeepSeekExample>()),
                ("■ GroqCloud Chat", serviceProvider.GetRequiredService<ChatExampleGroqCloud>()),
                ("■ Anthropic Chat", serviceProvider.GetRequiredService<ChatExampleAnthropic>()),
                ("■ xAI Chat", serviceProvider.GetRequiredService<ChatExampleXai>()),
                ("■ Ollama Chat", serviceProvider.GetRequiredService<ChatExampleOllama>()),
                ("■ McpClient example", serviceProvider.GetRequiredService<McpExample>()),
                ("■ McpAgent example", serviceProvider.GetRequiredService<McpAgentsExample>()),
                ("■ Chat with TTS example", serviceProvider.GetRequiredService<ChatWithTextToSpeechExample>()),
                ("■ McpAgent example", serviceProvider.GetRequiredService<McpAgentsExample>()),
                ("■ Chat with custom model ID", serviceProvider.GetRequiredService<ChatWithCustomModelIdExample>())
            ];
        }
    };
}