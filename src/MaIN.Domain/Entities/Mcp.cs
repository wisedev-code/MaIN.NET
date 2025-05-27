using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public class Mcp
{
    public required string Name { get; init; }
    public required List<string> Arguments { get; init; }
    public required string Command { get; init; }
    public required string Model { get; init; }
    public Dictionary<string, string> Properties { get; set; } = [];
    public BackendType? Backend { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
    
    public static Mcp NotSet => new Mcp()
    {
        Arguments = [],
        Command = string.Empty,
        Model = string.Empty,
        Properties = new Dictionary<string, string>(),
        Name = string.Empty,
        Backend = BackendType.Self
    };
}