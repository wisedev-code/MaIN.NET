using MaIN.Domain.Configuration;

namespace MaIN.Domain.Entities;

public class Mcp
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
    public BackendType Backend { get; set; }
}