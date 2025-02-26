using MaIN.Core.Hub;
using MaIN.Core.Interfaces;
using MaIN.Core.Services;
using MaIN.Domain.Configuration;
using MaIN.Services;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddMaIN(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MaINSettings>? configureSettings = null)
    {
        services.ConfigureMaIN(configuration, configureSettings);
        services.AddAIHub();
        return services;
    }

    public static IServiceProvider UseMaIN(this IServiceProvider sp)
    {
        _ = sp.GetRequiredService<IAIHubServices>();
        sp.InitializeAgents();
        return sp;
    }
    
    // public static WebApplication UseMaIN(this WebApplication sp)
    // {
    //     _ = sp.Services.GetRequiredService<IAIHubServices>();
    //     sp.Services.InitializeAgents();
    //     return sp;
    // } //TODO find a way to properly handle this to not require framework REF
    
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
        
        // Ensure IHttpClientFactory is registered
        if (services.All(sd => sd.ServiceType != typeof(IHttpClientFactory)))
        {
            services.AddHttpClient(); // Register the default HttpClientFactory
        }

        return services;
    }
}

public static class MaINBootstrapper
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the dependency injection container and registers all services.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureSettings">Optional settings configuration.</param>
    public static void Initialize(IConfiguration? configuration = null, Action<MaINSettings>? configureSettings = null)
    {
        // Create a new ServiceCollection
        var services = new ServiceCollection();

        // Build configuration if not provided
        if (configuration == null)
        {
            var basePath = Directory.GetCurrentDirectory();
            configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();


            // Use your existing extension method to register the MaIN services
            services.AddMaIN(configuration, configureSettings);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            // This call ensures that any initialization steps are performed (e.g. initializing agents)
            _serviceProvider.UseMaIN();

            Console.WriteLine("AIHub Initialized Successfully");
        }
    }
}