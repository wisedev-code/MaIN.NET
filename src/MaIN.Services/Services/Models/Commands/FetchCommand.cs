using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Models.Commands;

public class FetchCommand : BaseCommand, ICommand<Message?>
{
    public string? Filter { get; init; }
    public required AgentData Context { get; init; }
    public string CommandName => "FETCH_DATA";
    public Chat? MemoryChat { get; set; }
}