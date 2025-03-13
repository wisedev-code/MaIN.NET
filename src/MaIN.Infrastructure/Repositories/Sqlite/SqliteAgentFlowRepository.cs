using System.Data;
using System.Text.Json;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sqlite;

public class SqliteAgentFlowRepository(IDbConnection connection) : IAgentFlowRepository
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private AgentFlowDocument MapAgentFlowDocument(dynamic row)
    {
        var flow = new AgentFlowDocument
        {
            Id = row.Id,
            Name = row.Name,
            Agents = row.Agents != null ? 
                JsonSerializer.Deserialize<List<AgentDocument>>(row.Agents, _jsonOptions) : 
                new List<AgentDocument>(),
            Description = row.Description
        };
        return flow;
    }

    private object MapAgentFlowToParameters(AgentFlowDocument flow)
    {
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));

        return new
        {
            flow.Id,
            flow.Name,
            Agents = JsonSerializer.Serialize(flow.Agents ?? new List<AgentDocument>(), _jsonOptions),
            flow.Description,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows()
    {
        var rows = await connection.QueryAsync(
            "SELECT * FROM AgentFlows");
        return rows.Select(MapAgentFlowDocument);
    }

    public async Task<AgentFlowDocument?> GetFlowById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM AgentFlows WHERE Id = @Id",
            new { Id = id });
        return row != null ? MapAgentFlowDocument(row) : null;
    }

    public async Task AddFlow(AgentFlowDocument flow)
    {
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));

        var parameters = MapAgentFlowToParameters(flow);
        await connection.ExecuteAsync(@"
            INSERT INTO AgentFlows (
                Id, Name, Agents, Description, CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @Name, @Agents, @Description, @CreatedAt, @UpdatedAt
            )", parameters);
    }

    public async Task UpdateFlow(string id, AgentFlowDocument flow)
    {
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));

        var parameters = MapAgentFlowToParameters(flow);
        await connection.ExecuteAsync(@"
            UPDATE AgentFlows 
            SET Name = @Name,
                Agents = @Agents,
                Description = @Description,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id", parameters);
    }

    public async Task DeleteFlow(string id) =>
        await connection.ExecuteAsync(
            "DELETE FROM AgentFlows WHERE Id = @Id",
            new { Id = id });
}