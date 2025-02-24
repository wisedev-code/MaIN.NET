namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentNoSqlSourceDetails : IAgentSource
{
    public string ConnectionString { get; set; }
    public string DbName { get; set; }
    public string Collection { get; set; }
    public string Query { get; set; }
}