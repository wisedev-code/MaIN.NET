using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts.Interfaces.McpContext;

public interface IMcpContext
{
    /// <summary>
    /// Sets the MCP configuration for the context. This configuration defines the connection parameters and settings required
    /// to interact with MCP servers.
    /// </summary>
    /// <param name="mcpConfig">The <see cref="Mcp"/> configuration object containing server connection details and settings.</param>
    /// <returns>The context instance implementing <see cref="IMcpContext"/> for method chaining.</returns>
    IMcpContext WithConfig(Mcp mcpConfig);

    /// <summary>
    /// Specifies the backend type to be used for MCP operations. This allows you to select different backend implementations
    /// based on your requirements.
    /// </summary>
    /// <param name="backendType">The <see cref="BackendType"/> enum value specifying which backend implementation to use.</param>
    /// <returns>The context instance implementing <see cref="IMcpContext"/> for method chaining.</returns>
    IMcpContext WithBackend(BackendType backendType);

    /// <summary>
    /// Asynchronously processes a prompt through the configured MCP service, sending the prompt to the MCP server and returning the processed result.
    /// </summary>
    /// <param name="prompt">The text prompt to be processed by the MCP service</param>
    /// <returns>A <see cref="McpResult"/> object containing the processed response from the MCP server.</returns>
    Task<McpResult> PromptAsync(string prompt);
}