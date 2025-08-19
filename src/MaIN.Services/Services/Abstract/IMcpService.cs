using MaIN.Domain.Entities;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Abstract;

public interface IMcpService
{
    Task<McpResult> Prompt(Mcp config, List<Message> messageHistory);
}