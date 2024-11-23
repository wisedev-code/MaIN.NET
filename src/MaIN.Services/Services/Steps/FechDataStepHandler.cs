using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Steps;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps;

public class FetchDataStepHandler : IStepHandler
{
    public string StepName => "FETCH_DATA";

    public string[] SupportedSteps => ["FETCH_DATA", "FETCH_DATA*"];
    private static string _temporaryChatId = Guid.NewGuid().ToString();

    public async Task<StepResult> Handle(StepContext context)
    {
        if (context.StepName == "FETCH_DATA*" && context.Chat!.Properties.ContainsKey("FETCH_DATA*"))
        {
            return new StepResult { Chat = context.Chat };
        }

        var filterExists = context.Chat!.Properties.TryGetValue("data_filter", out var filter);
        var fetchCommand = new FetchCommand
        {
            Chat = context.Chat,
            Filter = filterExists ? filter : string.Empty,
            Context = context.Agent.Context.ToDomain()
        };

        var response = (await Actions.CallAsync("FETCH_DATA", fetchCommand) as Message)!;

        if (response.Properties.ContainsValue("JSON"))
        {
            await ProcessJsonResponse(response, context);
        }
        else
        {
            context.Chat.Messages?.Add(new Message
            {
                Role = "System",
                Content = $"Remember this data: {response.Content}",
                Properties = response.Properties
            });
        }

        if (context.StepName == "FETCH_DATA*")
        {
            context.Chat.Properties.Add("FETCH_DATA*", string.Empty);
        }

        return new StepResult { Chat = context.Chat, RedirectMessage = context.Chat!.Messages!.Last() };
    }

    private static async Task ProcessJsonResponse(Message response, StepContext context)
    {
        var splitter = new JsonChunker(maxTokens: 1000);
        var chunks = splitter.ChunkJson(response.Content).ToList();

        if (response.Properties.TryGetValue("chunk_limit", out var property))
        {
            chunks = chunks.Take(int.Parse(property!)).ToList();
        }

        for (var index = 0; index < chunks.Count; index++)
        {
            await ProcessChunk(chunks[index], index, chunks.Count, context, response.Properties);
        }
    }

    private static async Task ProcessChunk(string chunk, int index, int total, StepContext context,
        Dictionary<string, string> responseProperties)
    {
        await context.NotifyProgress("true", context.Agent.Id, $"{index + 1}/{total}",
            context.Agent.CurrentBehaviour);

        var addition = total == index + 1 ? "Process it" : "Process it and wait for next message";
        var message = $"{chunk} - {addition}";
        context.Chat!.Messages?.Add(new Message
        {
            Role = "User",
            Content = message,
            Properties = responseProperties,
        });

        var temporaryChat = new Chat
        {
            Id = _temporaryChatId,
            Model = context.Chat.Model,
            Messages = new List<Message>
            {
                context.Chat.Messages!.First(),
                new() { Role = "User", Content = message }
            }
        };
        
        var newMessage = await Actions.CallAsync("ANSWER", new AnswerCommand
        {
            Chat = temporaryChat,
            LastChunk = index == total - 1,
            TemporaryChat = true
        }) as Message;

        newMessage!.Properties = new() { { "agent_internal", "true" } };
        context.Chat.Messages?.Add(newMessage);
    }
}