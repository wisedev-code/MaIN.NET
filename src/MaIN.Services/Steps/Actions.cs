using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Models;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Steps;

public static class Actions
{
    public static Dictionary<string, Delegate> Steps { get; private set; }

    public static void Initialize(IOllamaService ollamaService)
    {
        Steps = new Dictionary<string, Delegate>
        {
            { "START", new Func<StartCommand, Task<Chat>>(async startCommand =>
            {
                var message = new Message()
                {
                    Content = startCommand.InitialPrompt,
                    Role = "system"
                };
                
                startCommand.Chat.Messages?.Add(message);
                var result = await ollamaService.Send(startCommand.Chat);
                return startCommand.Chat;
            })},

            { "REDIRECT", new Func<int, int, int>((a, b) => a + b) },
            { "FETCH_DATA_WITH_FILTER", new Func<int, int, int>((a, b) => a * b) },
            
            { "ANSWER", new Func<AnswerCommand, Task<Chat>>(async answerCommand =>
            {
                var result = await ollamaService.Send(answerCommand.Chat);
                answerCommand.Chat.Messages?.Add(result!.Message.ToDomain());

                return answerCommand.Chat;
            })},
        };
    }
    
    public static async Task<object?> CallAsync(string functionName, params object[] parameters)
    {
        if (Steps.TryGetValue(functionName, out var func))
        {
            var result = func.DynamicInvoke(parameters);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    return taskType.GetProperty("Result")?.GetValue(task);
                }
                return null;
            }
            return result;
        }
        throw new InvalidOperationException("Function not found.");
    }
}