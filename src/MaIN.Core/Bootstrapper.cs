using MaIN.Core.Hub;
using MaIN.Core.Hub.Skills;
using MaIN.Core.Interfaces;
using MaIN.Core.Services;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Infrastructure;
using MaIN.Services;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
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
        services.ConfigureInfrastructure(configuration);
        services.AddAIHub();
        return services;
    }

    public static IServiceProvider UseMaIN(this IServiceProvider sp)
    {
        _ = sp.GetRequiredService<IAIHubServices>();
        return sp;
    }
    
    public static IServiceCollection AddAIHub(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IAgentFlowService, AgentFlowService>();
        services.AddSingleton<IMcpService, McpService>();

        // Register built-in skill providers
        services.AddSingleton<IAgentSkillProvider, WebSearchSkillProvider>();
        services.AddSingleton<IAgentSkillProvider, RagExpertSkillProvider>();
        services.AddSingleton<IAgentSkillProvider, JournalistSkillProvider>();
        services.AddSingleton<IAgentSkillProvider, SummarizerSkillProvider>();
        services.AddSingleton<IAgentSkillProvider, McpToolCallerSkillProvider>();

        // Register service provider for AIHub
        services.AddSingleton<IAIHubServices>(sp =>
            {
                var aiServices = new AIHubServices(
                    sp.GetRequiredService<IChatService>(),
                    sp.GetRequiredService<IAgentService>(),
                    sp.GetRequiredService<IAgentFlowService>(),
                    sp.GetRequiredService<IMcpService>(),
                    sp.GetRequiredService<ISkillRegistry>(),
                    sp.GetRequiredService<ISkillComposer>()
                );

                var settings = sp.GetRequiredService<MaINSettings>();
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                // Initialize AIHub with the services
                AIHub.Initialize(aiServices, settings, httpClientFactory );
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
        // Snapshot any externally-loaded skills (e.g. from AddSkillsFromDirectory) so they
        // survive re-initialization with new settings (e.g. switching backend to OpenAI).
        var previousSkills = AIHub.GetCurrentSkills()?.ToList();

        var services = new ServiceCollection();

        if (configuration == null)
        {
            var basePath = Directory.GetCurrentDirectory();
            configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            services.AddMaIN(configuration, configureSettings);

            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.UseMaIN();

            // Re-register skills that existed before re-init (file-based, user-defined, etc.)
            if (previousSkills is { Count: > 0 })
            {
                var registry = _serviceProvider.GetRequiredService<ISkillRegistry>();
                foreach (var skill in previousSkills)
                    registry.Register(skill);
            }

            Console.WriteLine("AIHub Initialized Successfully");
        }
    }
}