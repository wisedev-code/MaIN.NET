using Dapper;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using System.Data;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlAgentFlowRepository(IDbConnection connection) : IAgentFlowRepository
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
            Agents = row.Agents is not null
                ? JsonSerializer.Deserialize<List<AgentDocument>>(row.Agents.ToString(), _jsonOptions)
                : new List<AgentDocument>(),
            Description = row.Description
        };
        return flow;
    }

    private object MapAgentFlowToParameters(AgentFlowDocument flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        return new
        {
            flow.Id,
            flow.Name,
            Agents = JsonSerializer.Serialize(flow.Agents ?? [], _jsonOptions),
            flow.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<AgentFlow>> GetAllFlows()
    {
        var rows = await connection.QueryAsync(@"
            SELECT * FROM AgentFlows");
        return rows.Select(MapAgentFlowDocument).Select(x => x.ToDomain());
    }

    public async Task<AgentFlow?> GetFlowById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(@"
            SELECT * FROM AgentFlows
            WHERE Id = @Id",
            new { Id = id });
        return row is not null ? MapAgentFlowDocument(row).ToDomain() : null;
    }

    public async Task AddFlow(AgentFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var parameters = MapAgentFlowToParameters(flow.ToDocument());
        await connection.ExecuteAsync(@"
            INSERT INTO AgentFlows (
                Id, Name, Agents, Description, CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @Name, @Agents, @Description, @CreatedAt, @UpdatedAt)",
            parameters);
    }

    public async Task UpdateFlow(string id, AgentFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var parameters = MapAgentFlowToParameters(flow.ToDocument());
        await connection.ExecuteAsync(@"
            UPDATE AgentFlows
            SET Name = @Name,
                Agents = @Agents,
                Description = @Description,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id",
            parameters);
    }

    public async Task DeleteFlow(string id) =>
        await connection.ExecuteAsync(@"
            DELETE FROM AgentFlows
            WHERE Id = @Id",
            new { Id = id });

    public async Task<IEnumerable<AgentFlow>> GetFlowsByAgentName(string agentName)
    {
        var rows = await connection.QueryAsync(@"
            SELECT *
            FROM AgentFlows
            WHERE EXISTS (
                SELECT 1
                FROM OPENJSON(Agents)
                WITH (Name nvarchar(max)) AS AgentData
                WHERE AgentData.Name = @agentName
            )",
            new { agentName });
        return rows.Select(MapAgentFlowDocument).Select(x => x.ToDomain());
    }
}
