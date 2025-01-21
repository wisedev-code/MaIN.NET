using MaIN.Core.Hub;
using MaIN.Core.Interfaces;
using MaIN.Core.Services;
using MaIN.Services;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace MaIN.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddMaIN(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ConfigureMaIN(configuration);
        services.AddAIHub();
        return services;
    }

    public static IServiceProvider UseMaINAgentFramework(IServiceProvider services)
    {
        services.InitializeAgents();
        return services;
    }

    public static WebApplication UseMaINAgentFramework(this WebApplication app)
    {
        app.Services.InitializeAgents();
        return app;
    }
    
    public static IServiceCollection AddAIHub(this IServiceCollection services)
    {
        //services.Configure(configureOptions);
        
        // Register core services
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IAgentFlowService, AgentFlowService>();
        
        // Register service provider for AIHub
        services.AddSingleton<IAIHubServices>(sp =>
            {
                var aiServices = new AIHubServices(
                    sp.GetRequiredService<IChatService>(),
                    sp.GetRequiredService<IAgentService>(),
                    sp.GetRequiredService<IAgentFlowService>()
                );
            
                // Initialize AIHub with the services
                AIHub.Initialize(aiServices);
                return aiServices;
            }
        );

        return services;
    }
}