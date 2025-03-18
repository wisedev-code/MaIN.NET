using MaIN.Services.Services.Models.Commands.Base;

namespace MaIN.Services.Services.Models.Commands;

public class BecomeCommand : BaseCommand
{
    public required string Key { get; set; }
}