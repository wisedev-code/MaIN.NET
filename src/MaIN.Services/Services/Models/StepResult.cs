using MaIN.Domain.Entities;

namespace MaIN.Services.Services.Models;

public class StepResult
{
    public Chat Chat { get; init; } = null!;
    public Message? RedirectMessage { get; init; }
}