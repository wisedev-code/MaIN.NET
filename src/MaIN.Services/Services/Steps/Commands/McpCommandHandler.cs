using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class McpCommandHandler(
    IMcpService mcpService)
    : ICommandHandler<McpCommand, Message?>
{
    public async Task<Message?> HandleAsync(McpCommand command)
    {
        var result = await mcpService.Prompt(command.McpConfig, command.Chat.Messages);
        return result.Message;
    }
}