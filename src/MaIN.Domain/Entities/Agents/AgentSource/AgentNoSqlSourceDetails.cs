namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentNoSqlSourceDetails : IAgentSource
{
    public required string ConnectionString { get; set; }
    public required string DbName { get; init; }
    public required string Collection { get; set; }
    public required string Query { get; set; }
}