using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Services.Services.Steps.Commands;

public class FetchCommandHandler(
    IHttpClientFactory httpClientFactory,
    IDataSourceProvider dataSourceService)
    : ICommandHandler<FetchCommand, Message?>
{
    public async Task<Message?> HandleAsync(FetchCommand command)
    {
        var properties = new Dictionary<string, string>
        {
            { "agent_internal", "true" }
        };

        string data;
        
        switch (command.Context.Source!.Type)
        {
            case AgentSourceType.File:
                data = await dataSourceService.FetchFileData(command.Context.Source.Details);
                break;
            case AgentSourceType.Text:
                data = dataSourceService.FetchTextData(command.Context.Source.Details);
                break;
            case AgentSourceType.API:
                data = await dataSourceService.FetchApiData(
                    command.Context.Source.Details,
                    command.Filter,
                    httpClientFactory,
                    properties);
                break;
            case AgentSourceType.SQL:
                data = await dataSourceService.FetchSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                break;
            case AgentSourceType.NoSQL:
                data = await dataSourceService.FetchNoSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // TODO if needed handle special case for "FETCH_DATA*"

        return new Message()
        {
            Content = data,
            Role = "User",
            Properties = properties
        };
    }
}