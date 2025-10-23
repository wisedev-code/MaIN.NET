using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Steps;

public class McpStepHandler(ICommandDispatcher commandDispatcher) : IStepHandler
{
    public string StepName => "MCP";

    public async Task<StepResult> Handle(StepContext context)
    {
        if (context.McpConfig == null)
        {
            throw new MissingFieldException("MCP config is missing");
        }
        
        await context.NotifyProgress("true", context.Agent.Id, null, context.Agent.CurrentBehaviour, StepName);
        var mcpCommand = new McpCommand()
        {
            Chat = StepHandlerExtensions.EnsureUserMessageReadiness(context.Chat), 
            McpConfig = context.McpConfig
        };
        
        var mcpResponse = await commandDispatcher.DispatchAsync(mcpCommand);
        if (mcpResponse == null)
        {
            throw new Exception("MCP command failed"); //TODO proper candidate for custom exception
        }

        var filterVal = GetFilter(mcpResponse.Content);
        if (!string.IsNullOrEmpty(filterVal))
        {
            context.Chat.Properties.TryAdd("data_filter", filterVal);
        }

        mcpResponse.Time = DateTime.Now;
        context.Chat.Messages.Add(mcpResponse);

        return new StepResult { Chat = context.Chat, RedirectMessage = mcpResponse };
    }

    private static string? GetFilter(string? content)
    {
        var pattern = @"filter:?:?\{(.*?)\}";        
        var match = Regex.Match(content!, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }
}