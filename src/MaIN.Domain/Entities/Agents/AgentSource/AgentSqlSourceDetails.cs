namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentSqlSourceDetails
{
    public string ConnectionString { get; set; }
    public string Table { get; set; }
    public string Query { get; set; }
}