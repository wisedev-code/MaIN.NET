using System.IO.Compression;
using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts;

public class McpContext
{
    private readonly IMcpService _mcpService;
    private BackendType _backendType;
    private Mcp? _mcpConfig;

    internal McpContext(IMcpService mcpService, Mcp config)
    {
        _mcpService = new McpService();
        _mcpConfig = config;
    }
    
    public McpContext WithBackend(BackendType backendType)
    {
        _mcpConfig!.Backend = backendType;
        return this;
    }

    public async Task<McpResult> PromptAsync(string prompt) => 
        await _mcpService.Prompt(_mcpConfig!, prompt);
}