using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Exceptions;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MaIN.Services.Services;

public class DataSourceProvider : IDataSourceProvider
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> FetchFileData(Dictionary<string, string> source)
    {
        var allContent = new StringBuilder();
    
        foreach (var (fileName, filePath) in source)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                allContent.AppendLine($"=== {fileName} ===");
                allContent.AppendLine(content);
                allContent.AppendLine();
            }
            catch (Exception)
            {
                allContent.AppendLine($"=== Error reading {fileName} ===");
                allContent.AppendLine();
            }
        }
    
        return allContent.ToString();
    }

    public string FetchTextData(object? sourceDetails)
    {
        if (sourceDetails is AgentTextSourceDetails textDetails)
        {
            return textDetails.Text;
        }

        return sourceDetails?.ToString() ?? string.Empty;
    }

    public async Task<string> FetchApiData(object? details, string? filter,
        IHttpClientFactory httpClientFactory, Dictionary<string, string> properties)
    {
        var apiDetails = JsonSerializer.Deserialize<AgentApiSourceDetails>(details!.ToString()!,
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });

        var httpClient = httpClientFactory.CreateClient();

        apiDetails!.Payload = apiDetails.Payload?.Replace("@filter@", filter);
        apiDetails.Query = apiDetails.Query?.Replace("@filter@", filter);
        apiDetails.Url = apiDetails.Url.Replace("@filter@", filter);

        var request = new HttpRequestMessage(
            HttpMethod.Parse(apiDetails?.Method),
            apiDetails?.Url + apiDetails?.Query);

        if (!string.IsNullOrEmpty(apiDetails?.Payload))
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(apiDetails.Payload),
                Encoding.UTF8,
                "application/json");
        }

        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
            throw new ApiRequestFailedException(
                result.StatusCode, 
                apiDetails?.Url + apiDetails?.Query, 
                apiDetails?.Method ?? string.Empty);
        }

        var data = await result.Content.ReadAsStringAsync();

        properties.TryAdd("api_response_type", apiDetails?.ResponseType ?? "JSON");
        if (apiDetails?.ChunkLimit != null)
        {
            properties.TryAdd("chunk_limit", apiDetails.ChunkLimit.ToString()!);
        }

        return apiDetails?.ResponseType == "HTML" ? HtmlContentCleaner.CleanHtml(data) : data;
    }

    public async Task<string> FetchSqlData(object? sourceDetails, string? filter, Dictionary<string, string> properties)
    {
        var sqlDetails = JsonSerializer.Deserialize<AgentSqlSourceDetails>(sourceDetails!.ToString()!);
        sqlDetails!.ConnectionString = sqlDetails.ConnectionString.Replace("@filter@", filter);
        sqlDetails.Query = sqlDetails.Query.Replace("@filter@", filter);

        await using SqlConnection connection = new SqlConnection(sqlDetails.ConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new SqlCommand(sqlDetails.Query, connection);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        var data = new List<Dictionary<string, object>>();

        if (!reader.HasRows) 
            return JsonSerializer.Serialize(data, JsonSerializerOptions);
        
        var columns = reader.GetColumnSchema();
        while (await reader.ReadAsync())
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            foreach (var column in columns)
            {
                row[column.ColumnName] = reader[column.ColumnName];
            }

            data.Add(row);
        }

        return JsonSerializer.Serialize(data, JsonSerializerOptions);
    }

    public async Task<string> FetchNoSqlData(object? sourceDetails, string? filter,
        Dictionary<string, string> properties)
    {
        var noSqlDetails = JsonSerializer.Deserialize<AgentNoSqlSourceDetails>(sourceDetails!.ToString()!);

        noSqlDetails!.ConnectionString = noSqlDetails.ConnectionString.Replace("@filter@", filter);
        noSqlDetails.Query = noSqlDetails.Query.Replace("@filter@", filter);
        noSqlDetails.Collection = noSqlDetails.Collection.Replace("@filter@", filter);

        var clientSettings = MongoClientSettings.FromConnectionString(noSqlDetails.ConnectionString);
        var client = new MongoClient(clientSettings);

        var database = client.GetDatabase(noSqlDetails.DbName);
        var collection = database.GetCollection<BsonDocument>(noSqlDetails.Collection);
        var bsonQuery = BsonDocument.Parse(noSqlDetails.Query);
        var mongoFilter = new BsonDocumentFilterDefinition<BsonDocument>(bsonQuery);

        var documents = await collection.Find(mongoFilter).ToListAsync();
        var data = new List<Dictionary<string, object>>();

        foreach (var document in documents)
        {
            var row = new Dictionary<string, object>();
            foreach (var element in document.Elements)
            {
                row[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
            }

            data.Add(row);
        }

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }
}