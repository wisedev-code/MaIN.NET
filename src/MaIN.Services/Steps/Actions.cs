using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Commands;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MaIN.Services.Steps;

public static class Actions
{
    public static Dictionary<string, Delegate> Steps { get; private set; }

    public static void InitializeAgents(this IServiceProvider serviceProvider)
    {
        var ollamaService = serviceProvider.GetRequiredService<IOllamaService>();
        var agentService = serviceProvider.GetRequiredService<IAgentService>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Steps = new Dictionary<string, Delegate>
        {
            {
                "START", new Func<StartCommand, Task<Message?>>(async startCommand =>
                {
                    var message = new Message()
                    {
                        Content = startCommand.InitialPrompt,
                        Role = "system"
                    };
                    startCommand.Chat?.Messages?.Add(message);
                    
                    var result = await ollamaService.Send(startCommand.Chat);
                    return result?.Message.ToDomain();
                })
            },

            {
                "REDIRECT", new Func<RedirectCommand, Task<Message?>>(async redirectCommand =>
                {
                    var chat = await agentService.GetChatByAgent(redirectCommand.RelatedAgentId);
                    chat.Messages?.Add(new Message()
                    {
                        Role = "user",
                        Content = redirectCommand.Message.Content //TODO: workaround to fake user input and make agent respond
                    });
                    var result = await agentService.Process(chat, redirectCommand.RelatedAgentId);
                    return result!.Messages?.Last();
                })
            },

            {
                "FETCH_DATA", new Func<FetchCommand, Task<Message?>>(async fetchCommand =>
                {
                    //TBD
                    var data = fetchCommand.Context.Source.Type switch
                    {
                        AgentSourceType.File => await File.ReadAllTextAsync(
                            ((AgentFileSourceDetails)fetchCommand.Context.Source.Details!).Path),
                        AgentSourceType.Text => ((AgentTextSourceDetails)fetchCommand.Context.Source.Details!).Text,
                        AgentSourceType.API => await FetchApiData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter, httpClientFactory),
                        AgentSourceType.SQL => await FetchSqlData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter),
                        AgentSourceType.NoSQL => await FetchNoSqlData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var dataMsg = new Message()
                    {
                        Content =
                            $"Here is data from internal data source, This is what you should use to answer questions: {data}",
                        Role = "system"
                    };

                    return dataMsg;
                })
            },
            
            { //TODO better handling for duplication
                "FETCH_DATA*", new Func<FetchCommand, Task<Message?>>(async fetchCommand =>
                {
                    //TBD
                    var data = fetchCommand.Context.Source.Type switch
                    {
                        AgentSourceType.File => await File.ReadAllTextAsync(
                            JsonSerializer.Deserialize<AgentFileSourceDetails>(fetchCommand.Context.Source.Details?.ToString()!)!.Path),
                        AgentSourceType.Text => fetchCommand.Context.Source.Details!.ToString(),
                        AgentSourceType.API => await FetchApiData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter, httpClientFactory),
                        AgentSourceType.SQL => await FetchSqlData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter),
                        AgentSourceType.NoSQL => await FetchNoSqlData(fetchCommand.Context.Source.Details,
                            fetchCommand.Filter),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var dataMsg = new Message()
                    {
                        Content =
                            $"Here is data from internal data source, This is what you should use to answer questions: {data}",
                        Role = "system"
                    };

                    return dataMsg;
                })
            },

            {
                "ANSWER", new Func<AnswerCommand, Task<Message?>>(async answerCommand =>
                {
                    var result = await ollamaService.Send(answerCommand.Chat);
                    return result!.Message.ToDomain();
                })
            },
        };
    }

    private static async Task<string> FetchNoSqlData(object? sourceDetails, string? fetchCommandFilter)
    {
        var noSqlDetails = JsonSerializer.Deserialize<AgentNoSqlSourceDetails>(sourceDetails.ToString());
        noSqlDetails!.ConnectionString = noSqlDetails.ConnectionString.Replace("@filter@", fetchCommandFilter);
        noSqlDetails.Query = noSqlDetails.Query.Replace("@filter@", fetchCommandFilter);
        noSqlDetails.Collection = noSqlDetails.Collection.Replace("@filter@", fetchCommandFilter);
        var clientSettings = MongoClientSettings.FromConnectionString(noSqlDetails!.ConnectionString);
        var client = new MongoClient(clientSettings);

        var database = client.GetDatabase(noSqlDetails.DbName);
        var collection = database.GetCollection<BsonDocument>(noSqlDetails.Collection);
        var bsonQuery = BsonDocument.Parse(noSqlDetails.Query);
        var filter = new BsonDocumentFilterDefinition<BsonDocument>(bsonQuery);

        var documents = collection.Find(filter).ToList();
        var data = new List<Dictionary<string, object>>();

        var row = new Dictionary<string, object>();
        foreach (var document in documents)
        {
            foreach (var element in document.Elements)
            {
                row[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
            }

            data.Add(row);
        }

        var jsonResult = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return jsonResult;
    }

    private static async Task<string> FetchSqlData(object? sourceDetails, string? fetchCommandFilter)
    {
        var sqlDetails = JsonSerializer.Deserialize<AgentSqlSourceDetails>(sourceDetails.ToString());
        sqlDetails!.ConnectionString = sqlDetails.ConnectionString.Replace("@filter@", fetchCommandFilter);
        sqlDetails.Query = sqlDetails.Query.Replace("@filter@", fetchCommandFilter);
        await using SqlConnection connection = new SqlConnection(sqlDetails!.ConnectionString);
        connection.Open();

        var command = new SqlCommand(sqlDetails.Query, connection);
        var reader = await command.ExecuteReaderAsync();
        var data = new List<Dictionary<string, object>>();
        if (reader.HasRows)
        {
            var columns = reader.GetColumnSchema();
            while (reader.Read())
            {
                Dictionary<string, object> row = new Dictionary<string, object>();

                foreach (var column in columns)
                {
                    row[column.ColumnName] = reader[column.ColumnName];
                }

                data.Add(row);
            }
        }

        await reader.CloseAsync();
        var jsonResult = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return jsonResult;
    }

    private static async Task<string> FetchApiData(object? details, string? filter,
        IHttpClientFactory httpClientFactory)
    {
        var apiDetails = JsonSerializer.Deserialize<AgentApiSourceDetails>(details.ToString(), new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var httpClient = httpClientFactory.CreateClient();
        apiDetails!.Payload = apiDetails.Payload?.Replace("@filter@", filter);
        apiDetails.Query = apiDetails.Query?.Replace("@filter@", filter);
        apiDetails.Url = apiDetails.Url.Replace("@filter@", filter);
        var result = await httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Parse(apiDetails?.Method), apiDetails?.Url + apiDetails?.Query)
            {
                Content = apiDetails?.Payload != null
                    ? new StringContent(JsonSerializer.Serialize(apiDetails.Payload), Encoding.UTF8, "application/json")
                    : null
            });

        return await result.Content.ReadAsStringAsync();
    }

    public static async Task<object?> CallAsync(string functionName, params object[] parameters)
    {
        if (Steps.TryGetValue(functionName, out var func))
        {
            var result = func.DynamicInvoke(parameters);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    return taskType.GetProperty("Result")?.GetValue(task);
                }

                return null;
            }

            return result;
        }

        throw new InvalidOperationException("Function not found.");
    }
}