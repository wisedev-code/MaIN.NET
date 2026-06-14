using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Core.Hub.Contexts.Interfaces.ChatContext;
using MaIN.Core.Hub.Contexts.Interfaces.FlowContext;
using MaIN.Core.Hub.Contexts.Interfaces.McpContext;
using MaIN.Core.Hub.Contexts.Interfaces.ModelContext;

namespace MaIN.Core.Hub;

/// <summary>
/// Instance-based entry point for MaIN.NET, intended to be injected via DI.
/// Register with <c>services.AddMaIN(configuration)</c> and inject as <c>IMaINHub</c>
/// in your controllers or services.
/// </summary>
public interface IMaINHub
{
    IChatBuilderEntryPoint Chat();
    IAgentBuilderEntryPoint Agent();
    IFlowContext Flow();
    IModelContext Model();
    IMcpContext Mcp();
}
