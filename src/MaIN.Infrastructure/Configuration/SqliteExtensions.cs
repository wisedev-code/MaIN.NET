using System.Data;
using Dapper;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Infrastructure.Repositories.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Infrastructure.Configuration;

public static class SqliteRegistrationExtensions
{
    public static IServiceCollection AddSqliteRepositories(
        this IServiceCollection services,
        string connectionString)
    {
        // Register SQLite connection
        services.AddScoped<IDbConnection>(_ =>
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        });

        // Register repositories
        services.AddScoped<IChatRepository, SqliteChatRepository>()
            .AddScoped<IAgentFlowRepository, SqliteAgentFlowRepository>()
            .AddScoped<IAgentRepository, SqliteAgentRepository>();

        InitializeSqliteDatabase(connectionString);
        return services;
    }
    
    
    public static void InitializeSqliteDatabase(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Create tables if they don't exist
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Chats (
                Id TEXT PRIMARY KEY,
                -- Add other fields based on ChatDocument properties
                CreatedAt TEXT,
                UpdatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS AgentFlows (
                Id TEXT PRIMARY KEY,
                -- Add other fields based on AgentFlowDocument properties
                CreatedAt TEXT,
                UpdatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS Agents (
                Id TEXT PRIMARY KEY,
                -- Add other fields based on AgentDocument properties
                CreatedAt TEXT,
                UpdatedAt TEXT
            );
        ");
    }
}