using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;

namespace MaIN.Services.Services.Models.Commands;

public class FetchCommand : BaseCommand, ICommand<Message?>
{
    public string? Filter { get; init; }
    public required AgentData Context { get; init; }
    public string CommandName => "FETCH_DATA";
    public Chat? MemoryChat { get; set; }
    public FetchResponseType ResponseType { get; set; }
}

public enum FetchResponseType
{
    AS_Answer = 1,
    AS_System = 2
}