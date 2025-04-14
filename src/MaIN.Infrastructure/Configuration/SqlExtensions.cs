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

        connection.Execute(@"""
            -- Create tables if they don't exist
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Chats]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[Chats] (
                    [Id] NVARCHAR(450) PRIMARY KEY,
                    [Name] NVARCHAR(MAX) NOT NULL,
                    [Model] NVARCHAR(MAX) NOT NULL,
                    [Messages] NVARCHAR(MAX) NOT NULL,  -- Stored as JSON array
                    [Type] NVARCHAR(MAX) NOT NULL,      -- Stored as JSON
                    [Properties] NVARCHAR(MAX) NULL,    -- Stored as JSON
                    [Visual] BIT NOT NULL DEFAULT 0,
                    [InferenceParams] NVARCHAR(MAX) NULL,    -- Stored as JSON,
                    [MemoryParams] NVARCHAR(MAX) NULL,    -- Stored as JSON
                    [Interactive] BIT NOT NULL DEFAULT 0
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AgentFlows]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[AgentFlows] (
                    [Id] NVARCHAR(450) PRIMARY KEY,
                    [Name] NVARCHAR(MAX) NOT NULL,
                    [Agents] NVARCHAR(MAX) NOT NULL,    -- Stored as JSON array
                    [Description] NVARCHAR(MAX) NULL
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Agents]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[Agents] (
                    [Id] NVARCHAR(450) PRIMARY KEY,
                    [Name] NVARCHAR(MAX) NOT NULL,
                    [Model] NVARCHAR(MAX) NOT NULL,
                    [Description] NVARCHAR(MAX) NULL,
                    [Started] BIT NOT NULL DEFAULT 0,
                    [Context] NVARCHAR(MAX) NULL,       -- Stored as JSON
                    [ChatId] NVARCHAR(450) NOT NULL,
                    [Order] INT NOT NULL DEFAULT 0,
                    [Behaviours] NVARCHAR(MAX) NULL,    -- Stored as JSON
                    [CurrentBehaviour] NVARCHAR(MAX) NULL,
                    [Flow] BIT NOT NULL DEFAULT 0
                    CONSTRAINT [FK_Agents_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [Chats]([Id])
                );
            END

            -- Create indexes
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Agents_ChatId' AND object_id = OBJECT_ID('Agents'))
            BEGIN
                CREATE INDEX [IX_Agents_ChatId] ON [dbo].[Agents]([ChatId]);
            END

            -- Add check constraints for JSON columns
            ALTER TABLE [dbo].[Chats] ADD CONSTRAINT [CK_Chats_Messages_JSON] 
                CHECK (ISJSON([Messages]) = 1);

            ALTER TABLE [dbo].[Chats] ADD CONSTRAINT [CK_Chats_Type_JSON] 
                CHECK (ISJSON([Type]) = 1);

            ALTER TABLE [dbo].[Chats] ADD CONSTRAINT [CK_Chats_Properties_JSON] 
                CHECK (CASE WHEN [Properties] IS NULL THEN 1 ELSE ISJSON([Properties]) END = 1);

            ALTER TABLE [dbo].[AgentFlows] ADD CONSTRAINT [CK_AgentFlows_Agents_JSON] 
                CHECK (ISJSON([Agents]) = 1);

            ALTER TABLE [dbo].[Agents] ADD CONSTRAINT [CK_Agents_Context_JSON] 
                CHECK (CASE WHEN [Context] IS NULL THEN 1 ELSE ISJSON([Context]) END = 1);

            ALTER TABLE [dbo].[Agents] ADD CONSTRAINT [CK_Agents_Behaviours_JSON] 
                CHECK (CASE WHEN [Behaviours] IS NULL THEN 1 ELSE ISJSON([Behaviours]) END = 1);
""");
    }
}