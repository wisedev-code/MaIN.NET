using MaIN.Domain.Entities;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services;

public interface IMcpService
{
    Task<McpResult> Prompt(Mcp config,string prompt);
}

public class McpService : IMcpService
{
    public Task<McpResult> Prompt(Mcp config, string prompt)
    {
        throw new NotImplementedException();
    }
}