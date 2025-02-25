using MaIN.Domain.Entities;

namespace MaIN.Services.Services.Models;

public class StepResult
{
    public Chat? Chat { get; init; }
    public Message? RedirectMessage { get; init; }
}