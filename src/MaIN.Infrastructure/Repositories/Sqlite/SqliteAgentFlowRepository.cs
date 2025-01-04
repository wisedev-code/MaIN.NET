using System.Data;
using Dapper;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.Sqlite;

public class SqliteAgentFlowRepository : IAgentFlowRepository
{
    private readonly IDbConnection _connection;

    public SqliteAgentFlowRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows() =>
        await _connection.QueryAsync<AgentFlowDocument>("SELECT * FROM AgentFlows");

    public async Task<AgentFlowDocument> GetFlowById(string id) =>
        await _connection.QueryFirstOrDefaultAsync<AgentFlowDocument>(
            "SELECT * FROM AgentFlows WHERE Id = @Id",
            new { Id = id });

    public async Task AddFlow(AgentFlowDocument flow) =>
        await _connection.ExecuteAsync(
            "INSERT INTO AgentFlows (Id, /* other fields */) VALUES (@Id, /* other params */)",
            flow);

    public async Task UpdateFlow(string id, AgentFlowDocument flow) =>
        await _connection.ExecuteAsync(
            "UPDATE AgentFlows SET /* fields = @values */ WHERE Id = @Id",
            flow);

    public async Task DeleteFlow(string id) =>
        await _connection.ExecuteAsync(
            "DELETE FROM AgentFlows WHERE Id = @Id",
            new { Id = id });
}