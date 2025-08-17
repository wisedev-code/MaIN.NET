namespace MaIN.Domain.Entities;

public class Voice
{
    public required string Name { get; set; }
    public required float[,,] Features { get; set; }
}