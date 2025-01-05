using System.Data;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sql;

public class SqlAgentFlowRepository : IAgentFlowRepository
{
    private readonly IDbConnection _connection;

    public SqlAgentFlowRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows() =>
        await _connection.QueryAsync<AgentFlowDocument>(@"
            SELECT * FROM AgentFlows");

    public async Task<AgentFlowDocument> GetFlowById(string id) =>
        await _connection.QueryFirstOrDefaultAsync<AgentFlowDocument>(@"
            SELECT * FROM AgentFlows 
            WHERE Id = @Id", new { Id = id });

    public async Task AddFlow(AgentFlowDocument flow)
    {
        var sql = @"
            INSERT INTO AgentFlows (Id, CreatedAt, UpdatedAt) 
            VALUES (@Id, @CreatedAt, @UpdatedAt)";
            
        await _connection.ExecuteAsync(sql, flow);
    }

    public async Task UpdateFlow(string id, AgentFlowDocument flow)
    {
        var sql = @"
            UPDATE AgentFlows 
            SET UpdatedAt = @UpdatedAt 
            WHERE Id = @Id";
            
        await _connection.ExecuteAsync(sql, flow);
    }

    public async Task DeleteFlow(string id) =>
        await _connection.ExecuteAsync(@"
            DELETE FROM AgentFlows 
            WHERE Id = @Id", new { Id = id });
}