namespace MaIN.Services.Services.Abstract;

public interface IDataSourceProvider
{
    Task<string> FetchFileData(object? sourceDetails);
    string FetchTextData(object? sourceDetails);
    Task<string> FetchApiData(object? details, string? filter, IHttpClientFactory httpClientFactory, Dictionary<string, string> properties);
    Task<string> FetchSqlData(object? sourceDetails, string? filter, Dictionary<string, string> properties);
    Task<string> FetchNoSqlData(object? sourceDetails, string? filter, Dictionary<string, string> properties);
}