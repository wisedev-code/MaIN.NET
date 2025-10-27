using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models.Commands.Base;
using MaIN.Services.Services.Steps.Commands;

namespace MaIN.Services.Services.Models.Commands;

public class AnswerCommand : BaseCommand, ICommand<Message?>
{
    public bool LastChunk { get; set; }
    public bool TemporaryChat { get; set; }
    public required string AgentId { get; set; }
    public KnowledgeUsage KnowledgeUsage { get; init; }
    public Knowledge? Knowledge { get; init; }
    public string CommandName => "ANSWER";
    public Func<LLMTokenValue, Task>? Callback { get; set; }
}