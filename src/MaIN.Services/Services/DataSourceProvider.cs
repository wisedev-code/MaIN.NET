using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

        
        if (!apiDetails?.AuthenticationToken.IsNullOrEmpty() ?? false)
        {
            if (!apiDetails.AuthenticationType.HasValue)
                throw new InvalidOperationException("Please specify an authorization type");

            switch (apiDetails.AuthenticationType)
            {
                case AuthTypeEnum.Bearer:
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiDetails.AuthenticationToken);
                    break;
                
                case AuthTypeEnum.ApiKey:
                    request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiDetails.AuthenticationToken);
                    break;

                case AuthTypeEnum.Basic:
                    if (string.IsNullOrEmpty(apiDetails.UserName) || string.IsNullOrEmpty(apiDetails.UserPassword))
                        throw new InvalidOperationException("Username and password are required for basic authentication.");

                    var credentials = $"{apiDetails.UserName}:{apiDetails.UserPassword}";
                    var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Basic", base64Credentials);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported authentication type: {apiDetails.AuthenticationType}");
            }
        }

        if (!string.IsNullOrEmpty(apiDetails?.Curl))
        {
            CurlRequestParser.PopulateRequestFromCurl(request, apiDetails.Curl);
        }
        else
        {
            if (!string.IsNullOrEmpty(apiDetails?.Payload))
            {

                var jsonString = apiDetails.Payload;

                if (!(apiDetails.Payload is string))
                {
                    jsonString = JsonSerializer.Serialize(apiDetails.Payload);

                }
                else
                {
                    try
                    {
                        JsonDocument.Parse(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        try
                        {
                            // Attempt to wrap malformed payload into a JSON object as a last resort

                            jsonString = JsonSerializer.Serialize(apiDetails.Payload);
                            if (!jsonString.StartsWith('{'))
                            {
                                jsonString = $"{{{jsonString}}}";
                            }
                            jsonString = JsonCleaner.CleanAndUnescape(jsonString);

                            JsonDocument.Parse(jsonString!);
                        }
                        catch
                        {
                            throw new Exception($"Invalid JSON: {ex.Message}");
                        }

                        
                    }
                }

                request.Content = new StringContent(
                    jsonString!,
                    Encoding.UTF8,
                    "application/json");


            }

            
        }
        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception(
                $"API request failed with status code: {result.StatusCode}"); //TODO candidate for domain exception
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