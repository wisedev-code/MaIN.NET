using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Exceptions.MPC;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts;

public sealed class McpContext
{
    private readonly IMcpService _mcpService;
    private Mcp? _mcpConfig;

    internal McpContext(IMcpService mcpService)
    {
        _mcpService = mcpService;
        _mcpConfig = Mcp.NotSet;
    }
    
    public McpContext WithConfig(Mcp mcpConfig)
    {
        _mcpConfig = mcpConfig;
        return this;
    }
    
    public McpContext WithBackend(BackendType backendType)
    {
        _mcpConfig!.Backend = backendType;
        return this;
    }

    public async Task<McpResult> PromptAsync(string prompt)
    {
        if (_mcpConfig == null)
        {
            throw new MPCConfigNotFoundException();
        }
        
        return await _mcpService.Prompt(_mcpConfig!, [new Message()
        {
            Content = prompt,
            Role = ServiceConstants.Roles.User,
            Type = MessageType.CloudLLM
        }]);
    }
}