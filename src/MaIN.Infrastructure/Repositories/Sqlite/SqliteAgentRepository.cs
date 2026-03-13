using Dapper;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Agents;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Domain.Repositories;
using System.Data;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.Sqlite;

public class SqliteAgentRepository(IDbConnection connection) : IAgentRepository
{
    private readonly JsonSerializerOptions? _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private AgentDocument MapAgentDocument(dynamic row)
    {
        var agent = new AgentDocument
        {
            Id = row.Id,
            Name = row.Name,
            Model = row.Model,
            Description = row.Description,
            Started = Convert.ToBoolean((int)row.Started),
            Config = row.Context is not null
                ? JsonSerializer.Deserialize<AgentConfigDocument>(row.Context, _jsonOptions)
                : null,
            ChatId = row.ChatId,
            Order = (int)row.Order,
            Backend = (BackendType)row.BackendType,
            Behaviours = row.Behaviours is not null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(row.Behaviours, _jsonOptions)
                : new Dictionary<string, string>(),
            CurrentBehaviour = row.CurrentBehaviour,
            Flow = Convert.ToBoolean((int)row.Flow)
        };
        return agent;
    }

    private object MapAgentToParameters(AgentDocument agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return new
        {
            agent.Id,
            agent.Name,
            agent.Model,
            agent.Description,
            Started = agent.Started ? 1 : 0,
            Context = agent.Config is not null ? JsonSerializer.Serialize(agent.Config, _jsonOptions) : null,
            agent.ChatId,
            agent.Order,
            BackendType = agent.Backend,
            Behaviours = agent.Behaviours is not null ? JsonSerializer.Serialize(agent.Behaviours, _jsonOptions) : null,
            agent.CurrentBehaviour,
            Flow = agent.Flow ? 1 : 0
        };
    }

    public async Task<IEnumerable<Agent>> GetAllAgents()
    {
        var rows = await connection.QueryAsync(
            "SELECT * FROM Agents");
        return rows.Select(MapAgentDocument).Select(x => x.ToDomain());
    }

    public async Task<Agent?> GetAgentById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM Agents WHERE Id = @Id",
            new { Id = id });
        return row is not null ? MapAgentDocument(row).ToDomain() : null;
    }

    public async Task AddAgent(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var parameters = MapAgentToParameters(agent.ToDocument());
        await connection.ExecuteAsync(@"
            INSERT INTO Agents (
                Id, Name, Model, Description, Started, Context,
                ChatId, [Order], Behaviours, CurrentBehaviour, Flow
            ) VALUES (
                @Id, @Name, @Model, @Description, @Started, @Context,
                @ChatId, @Order, @Behaviours, @CurrentBehaviour, @Flow
            )", parameters);
    }

    public async Task UpdateAgent(string id, Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var parameters = MapAgentToParameters(agent.ToDocument());
        await connection.ExecuteAsync(@"
            UPDATE Agents
            SET Name = @Name,
                Model = @Model,
                Description = @Description,
                Started = @Started,
                Context = @Context,
                ChatId = @ChatId,
                [Order] = @Order,
                Behaviours = @Behaviours,
                CurrentBehaviour = @CurrentBehaviour,
                Flow = @Flow
            WHERE Id = @Id", parameters);
    }

    public async Task DeleteAgent(string id) =>
        await connection.ExecuteAsync(
            "DELETE FROM Agents WHERE Id = @Id",
            new { Id = id });

    public async Task<bool> Exists(string id)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Agents WHERE Id = @Id",
            new { Id = id });
        return count > 0;
    }
}
