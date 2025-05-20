using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public class Mcp
{
    public required string Name { get; set; }
    public required List<string> Arguments { get; set; }
    public required string Command { get; set; }
    public required string Model { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
    public BackendType? Backend { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}