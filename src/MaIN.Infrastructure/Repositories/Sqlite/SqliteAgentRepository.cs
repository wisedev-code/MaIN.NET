using System.Data;
using System.Text.Json;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

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
            Context = row.Context != null ? 
                JsonSerializer.Deserialize<AgentContextDocument>(row.Context, _jsonOptions) : 
                null,
            ChatId = row.ChatId,
            Order = (int)row.Order,
            Behaviours = row.Behaviours != null ? 
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Behaviours, _jsonOptions) : 
                new Dictionary<string, string>(),
            CurrentBehaviour = row.CurrentBehaviour,
            Flow = Convert.ToBoolean((int)row.Flow)
        };
        return agent;
    }

    private object MapAgentToParameters(AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        return new
        {
            agent.Id,
            agent.Name,
            agent.Model,
            agent.Description,
            Started = agent.Started ? 1 : 0,
            Context = agent.Context != null ? 
                JsonSerializer.Serialize(agent.Context, _jsonOptions) : null,
            agent.ChatId,
            agent.Order,
            Behaviours = agent.Behaviours != null ? 
                JsonSerializer.Serialize(agent.Behaviours, _jsonOptions) : null,
            agent.CurrentBehaviour,
            Flow = agent.Flow ? 1 : 0
        };
    }

    public async Task<IEnumerable<AgentDocument>> GetAllAgents()
    {
        var rows = await connection.QueryAsync(
            "SELECT * FROM Agents");
        return rows.Select(MapAgentDocument);
    }

    public async Task<AgentDocument?> GetAgentById(string id)
    {
        var row = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM Agents WHERE Id = @Id",
            new { Id = id });
        return row != null ? MapAgentDocument(row) : null;
    }

    public async Task AddAgent(AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var parameters = MapAgentToParameters(agent);
        await connection.ExecuteAsync(@"
            INSERT INTO Agents (
                Id, Name, Model, Description, Started, Context, 
                ChatId, [Order], Behaviours, CurrentBehaviour, Flow
            ) VALUES (
                @Id, @Name, @Model, @Description, @Started, @Context,
                @ChatId, @Order, @Behaviours, @CurrentBehaviour, @Flow
            )", parameters);
    }

    public async Task UpdateAgent(string id, AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var parameters = MapAgentToParameters(agent);
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