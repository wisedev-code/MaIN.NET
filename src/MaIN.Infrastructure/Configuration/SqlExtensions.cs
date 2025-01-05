using System.Data;
using Dapper;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Infrastructure.Repositories.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Infrastructure.Configuration;

public static class SqlRegistrationExtensions
{
    public static IServiceCollection AddSqlRepositories(
        this IServiceCollection services,
        string connectionString)
    {
        // Register SQL connection
        services.AddScoped<IDbConnection>(_ =>
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        });

        // Register repositories
        services.AddScoped<IChatRepository, SqlChatRepository>()
            .AddScoped<IAgentFlowRepository, SqlAgentFlowRepository>()
            .AddScoped<IAgentRepository, SqlAgentRepository>();

        InitializeSqlDatabase(connectionString);
        
        return services;
    }

    public static void InitializeSqlDatabase(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        connection.Execute(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Chats' and xtype='U')
            CREATE TABLE Chats (
                Id NVARCHAR(450) PRIMARY KEY,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2 NOT NULL
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AgentFlows' and xtype='U')
            CREATE TABLE AgentFlows (
                Id NVARCHAR(450) PRIMARY KEY,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2 NOT NULL
            );

            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Agents' and xtype='U')
            CREATE TABLE Agents (
                Id NVARCHAR(450) PRIMARY KEY,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2 NOT NULL
            );");
    }
}