using MaIN.Core.Hub.Contexts;

namespace MaIN.Core.Hub;

/// <summary>
/// Instance-based entry point for MaIN.NET, intended to be injected via DI.
/// Register with <c>services.AddMaIN(configuration)</c> and inject as <c>IMaINHub</c>
/// in your controllers or services.
/// </summary>
public interface IMaINHub
{
    ChatContext Chat();
    AgentContext Agent();
    FlowContext Flow();
    ModelContext Model();
    McpContext Mcp();
}
