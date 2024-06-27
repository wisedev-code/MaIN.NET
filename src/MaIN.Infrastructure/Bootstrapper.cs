using MaIN.Infrastructure.Configuration;
using MaIN.Infrastructure.Providers.cs;
using MaIN.Infrastructure.Providers.cs.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MaIN.Infrastructure;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services,
        ConfigurationManager configuration)
    {
        if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Docker")
        {
            services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient("mongodb://mongodb:27017"));
        }
        else
        {
            services.AddSingleton<IMongoClient, MongoClient>(sp =>
                new MongoClient(configuration.GetSection("MongoDbSettings:ConnectionString").Value));
        }

        services.AddScoped<IChatProvider, ChatProvider>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var database = mongoClient.GetDatabase(configuration.GetSection("MongoDbSettings:DatabaseName").Value);
            return new ChatProvider(database, configuration.GetSection("MongoDbSettings:CollectionName").Value!);
        });

        return services;
    }
}