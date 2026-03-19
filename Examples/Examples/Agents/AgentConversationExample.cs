using MaIN.Core.Hub;

namespace Examples.Agents;

public class AgentConversationExample : IExample
{
    private static readonly ConsoleColor _userColor = ConsoleColor.Magenta;
    private static readonly ConsoleColor _agentColor = ConsoleColor.Green;
    private static readonly ConsoleColor _systemColor = ConsoleColor.Yellow;

    public async Task Start()
    {
        PrintColored("Agent conversation example is running!", _systemColor);

        PrintColored("Enter agent name: ", _systemColor, false);
        var agentName = Console.ReadLine();

        PrintColored("Enter agent profile (example: 'Gentle and helpful assistant'): ", _systemColor, false);
        var agentProfile = Console.ReadLine();

        PrintColored("Enter LLM model (ex: gemma3:4b, llama3.2:3b, yi:6b): ", _systemColor, false);
        var model = Console.ReadLine()!;
        var systemPrompt =
            $"""
             Your name is: {agentName}
             You are: {agentProfile}
             Always stay in your role.
             """;

        PrintColored($"Creating agent '{agentName}' with profile: '{agentProfile}' using model: '{model}'", _systemColor);
        AIHub.Extensions.DisableLLamaLogs();
        AIHub.Extensions.DisableNotificationsLogs();
        var context = await AIHub.Agent()
            .WithModel(model)
            .WithInitialPrompt(systemPrompt)
            .CreateAsync(interactiveResponse: true);

        bool conversationActive = true;
        while (conversationActive)
        {
            PrintColored("You > ", _userColor, false);
            string userMessage = Console.ReadLine()!;

            if (userMessage.ToLower() is "exit" or "quit")
            {
                conversationActive = false;
                continue;
            }

            PrintColored($"{agentName} > ", _agentColor, false);
            await context.ProcessAsync(userMessage);

            Console.WriteLine();
        }

        PrintColored("Conversation ended. Goodbye!", _systemColor);
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
