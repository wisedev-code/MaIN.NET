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
            Name TEXT NOT NULL,
            Model TEXT NOT NULL,
            Messages TEXT NOT NULL,  -- Stored as JSON array
            Type TEXT NOT NULL,      -- Stored as JSON
            Properties TEXT,         -- Stored as JSON
            Visual INTEGER NOT NULL DEFAULT 0,
            BackendType INTEGER NOT NULL DEFAULT 0,
            ConvState TEXT,         -- Stored as JSON
            InferenceParams TEXT,         -- Stored as JSON
            MemoryParams TEXT,         -- Stored as JSON
            Interactive INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS AgentFlows (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Agents TEXT NOT NULL,    -- Stored as JSON array
            Description TEXT
        );

        CREATE TABLE IF NOT EXISTS Agents (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Model TEXT NOT NULL,
            Description TEXT,
            Started INTEGER NOT NULL DEFAULT 0,
            Context TEXT,           -- Stored as JSON
            ChatId TEXT NOT NULL,
            [Order] INTEGER NOT NULL DEFAULT 0,
            BackendType INTEGER NOT NULL DEFAULT 0,
            Behaviours TEXT,        -- Stored as JSON
            CurrentBehaviour TEXT,
            Flow INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (ChatId) REFERENCES Chats(Id)
        );
    ");
    }
}