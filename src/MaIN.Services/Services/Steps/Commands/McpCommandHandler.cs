using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;

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