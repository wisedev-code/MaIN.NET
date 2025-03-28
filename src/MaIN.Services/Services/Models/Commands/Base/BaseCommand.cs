using MaIN.Domain.Entities;

namespace MaIN.Services.Services.Models.Commands.Base;

public class BaseCommand
{
    public required Chat Chat { get; init; }
}