using MaIN.Domain.Configuration;
using MaIN.Infrastructure.Configuration;
using MaIN.Infrastructure.Repositories;
using MaIN.Infrastructure.Repositories.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MaIN.Infrastructure;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        var settings = new MainSettings();
        configuration.GetSection("MaIN").Bind(settings);
        services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient(settings.MongoDbSettings?.ConnectionString));
        
        services.AddSingleton<IChatRepository, ChatRepository>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase(settings.MongoDbSettings?.DatabaseName!);
            return new ChatRepository(database, settings.MongoDbSettings?.ChatsCollection!);
        });
        
        services.AddSingleton<IAgentRepository, AgentRepository>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase(settings.MongoDbSettings?.DatabaseName!);
            return new AgentRepository(database, settings.MongoDbSettings?.AgentsCollection!);
        });
        
        services.AddSingleton<IAgentFlowRepository, AgentFlowRepository>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase(settings.MongoDbSettings?.DatabaseName!);
            return new AgentFlowRepository(database, settings.MongoDbSettings?.FlowsCollection!);
        });

        return services;
    }
}