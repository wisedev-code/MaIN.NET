using System.Data;
using System.Text.Json;
using Dapper;
using MaIN.Domain.Configuration;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlAgentRepository : IAgentRepository
{
    private readonly IDbConnection _connection;
    private readonly JsonSerializerOptions? _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SqlAgentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    private AgentDocument MapAgentDocument(dynamic row)
    {
        var agent = new AgentDocument
        {
            Id = row.Id,
            Name = row.Name,
            Model = row.Model,
            Description = row.Description,
            Started = row.Started,
            Context = row.Context != null ? 
                JsonSerializer.Deserialize<AgentContextDocument>(row.Context.ToString(), _jsonOptions) : 
                null,
            ChatId = row.ChatId,
            Order = row.Order,
            Backend = (BackendType)row.BackendType,
            Behaviours = row.Behaviours != null ? 
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Behaviours.ToString(), _jsonOptions) : 
                new Dictionary<string, string>(),
            CurrentBehaviour = row.CurrentBehaviour,
            Flow = row.Flow
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
            agent.Started,
            Context = agent.Context != null ? 
                JsonSerializer.Serialize(agent.Context, _jsonOptions) : null,
            agent.ChatId,
            agent.Order,
            BackendType = agent.Backend ?? 0,
            Behaviours = agent.Behaviours != null ? 
                JsonSerializer.Serialize(agent.Behaviours, _jsonOptions) : null,
            agent.CurrentBehaviour,
            agent.Flow,
        };
    }

    public async Task<IEnumerable<AgentDocument>> GetAllAgents()
    {
        var rows = await _connection.QueryAsync(@"
            SELECT * FROM Agents");
        return rows.Select(MapAgentDocument);
    }

    public async Task<AgentDocument?> GetAgentById(string id)
    {
        var row = await _connection.QueryFirstOrDefaultAsync(@"
            SELECT * FROM Agents 
            WHERE Id = @Id",
            new { Id = id });
        return row != null ? MapAgentDocument(row) : null;
    }

    public async Task AddAgent(AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var parameters = MapAgentToParameters(agent);
        await _connection.ExecuteAsync(@"
            INSERT INTO Agents (
                Id, Name, Model, Description, Started, Context,
                ChatId, [Order], Behaviours, CurrentBehaviour, Flow,
                CreatedAt, UpdatedAt
            ) VALUES (
                @Id, @Name, @Model, @Description, @Started, @Context,
                @ChatId, @Order, @Behaviours, @CurrentBehaviour, @Flow,
                @CreatedAt, @UpdatedAt)",
            parameters);
    }

    public async Task UpdateAgent(string id, AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var parameters = MapAgentToParameters(agent);
        await _connection.ExecuteAsync(@"
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
                Flow = @Flow,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id",
            parameters);
    }

    public async Task DeleteAgent(string id) =>
        await _connection.ExecuteAsync(@"
            DELETE FROM Agents 
            WHERE Id = @Id",
            new { Id = id });

    public async Task<bool> Exists(string id)
    {
        var count = await _connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) 
            FROM Agents 
            WHERE Id = @Id",
            new { Id = id });
        return count > 0;
    }

    // Optional: Add methods for JSON-specific queries
    public async Task<IEnumerable<AgentDocument?>> GetAgentsByBehaviour(string key, string value)
    {
        var rows = await _connection.QueryAsync(@"
            SELECT *
            FROM Agents
            WHERE JSON_VALUE(Behaviours, '$." + key + @"') = @value",
            new { value });
        return rows.Select(MapAgentDocument);
    }
}