using MaIN.Domain.Entities;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Models.Commands;

public class McpCommand : BaseCommand, ICommand<Message?>
{
    public string CommandName => "MCP";
    public required Mcp McpConfig { get; set; }
}