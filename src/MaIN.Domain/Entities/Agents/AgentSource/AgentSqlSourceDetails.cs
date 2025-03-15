namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentSqlSourceDetails : IAgentSource
{
    public required string ConnectionString { get; set; }
    public required string Table { get; init; }
    public required string Query { get; set; }
}