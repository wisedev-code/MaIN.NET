using System.Data;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlAgentRepository : IAgentRepository
{
    private readonly IDbConnection _connection;

    public SqlAgentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<AgentDocument?>> GetAllAgents() =>
        await _connection.QueryAsync<AgentDocument>(@"
            SELECT * FROM Agents");

    public async Task<AgentDocument?> GetAgentById(string id) =>
        await _connection.QueryFirstOrDefaultAsync<AgentDocument>(@"
            SELECT * FROM Agents 
            WHERE Id = @Id", new { Id = id });

    public async Task AddAgent(AgentDocument? agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var sql = @"
            INSERT INTO Agents (Id, CreatedAt, UpdatedAt) 
            VALUES (@Id, @CreatedAt, @UpdatedAt)";
            
        await _connection.ExecuteAsync(sql, agent);
    }

    public async Task UpdateAgent(string id, AgentDocument? agent)
    {
        var sql = @"
            UPDATE Agents 
            SET UpdatedAt = @UpdatedAt 
            WHERE Id = @Id";
            
        await _connection.ExecuteAsync(sql, agent);
    }

    public async Task DeleteAgent(string id) =>
        await _connection.ExecuteAsync(@"
            DELETE FROM Agents 
            WHERE Id = @Id", new { Id = id });

    public async Task<bool> Exists(string id)
    {
        var count = await _connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) 
            FROM Agents 
            WHERE Id = @Id", new { Id = id });
            
        return count > 0;
    }
}
