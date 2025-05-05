using MaIN.Core.Hub;

namespace Examples.Agents;

public class AgentConversationExample : IExample
{
    private static readonly ConsoleColor UserColor = ConsoleColor.Cyan;
    private static readonly ConsoleColor AgentColor = ConsoleColor.Green;
    private static readonly ConsoleColor SystemColor = ConsoleColor.Yellow;

    public async Task Start()
    {
        PrintColored("Agent conversation example is running!", SystemColor);
        
        PrintColored("Enter agent name: ", SystemColor, false);
        var agentName = Console.ReadLine();
        
        PrintColored("Enter agent profile (example: 'Gentle and helpful assistant'): ", SystemColor, false);
        var agentProfile = Console.ReadLine();
        
        PrintColored("Enter LLM model (ex: gemma3:4b, llama3.2:3b, yi:6b): ", SystemColor, false);
        var model = Console.ReadLine()!;
        var systemPrompt =
            $"""
             Your name is: {agentName}
             You are: {agentProfile}
             Always stay in your role.
             """;

        PrintColored($"Creating agent '{agentName}' with profile: '{agentProfile}' using model: '{model}'", SystemColor);
        AIHub.Extensions.DisableLLamaLogs();
        AIHub.Extensions.DisableNotificationsLogs();
        var context = await AIHub.Agent()
            .WithModel(model)
            .WithInitialPrompt(systemPrompt)
            .CreateAsync(interactiveResponse: true);
        
        bool conversationActive = true;
        while (conversationActive)
        {
            PrintColored("You > ", UserColor, false);
            string userMessage = Console.ReadLine()!;
            
            if (userMessage.ToLower() == "exit" || userMessage.ToLower() == "quit")
            {
                conversationActive = false;
                continue;
            }
            
            PrintColored($"{agentName} > ", AgentColor, false);
            await context.ProcessAsync(userMessage);
            
            Console.WriteLine(); 
        }
        
        PrintColored("Conversation ended. Goodbye!", SystemColor);
    }
    
    private static void PrintColored(string message, ConsoleColor color, bool newLine = true)
    {
        Console.ForegroundColor = color;
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }
        Console.ResetColor();
    }
}