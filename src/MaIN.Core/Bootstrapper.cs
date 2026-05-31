using MaIN.Core.Hub;
using MaIN.Core.Hub.Skills;
using MaIN.Core.Interfaces;
using MaIN.Core.Services;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions;
using MaIN.Infrastructure;
using MaIN.Services;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Warm the on-disk skills cache so the first lazy upload doesn't pay file-read latency.
        // No network work happens here — actual uploads run on demand from AgentContext.
        try
        {
            sp.UploadSkillsToProvidersAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Coordinator never throws today, but guard the bootstrap path anyway.
        }

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

        services.AddScoped<IMaINHub, MaINHub>();

        // Register service provider for AIHub (kept for [Obsolete] backward compat)
        services.AddSingleton<IAIHubServices>(sp =>
            {
                var aiServices = new AIHubServices(
                    sp.GetRequiredService<IChatService>(),
                    sp.GetRequiredService<IAgentService>(),
                    sp.GetRequiredService<IAgentFlowService>(),
                    sp.GetRequiredService<IMcpService>(),
                    sp.GetRequiredService<ISkillRegistry>(),
                    sp.GetRequiredService<ISkillComposer>(),
                    sp.GetService<MaIN.Services.Services.Skills.ProviderSkillUploadCoordinator>()
                );

                var settings = sp.GetRequiredService<MaINSettings>();
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

#pragma warning disable CS0618
                AIHub.Initialize(aiServices, settings, httpClientFactory);
#pragma warning restore CS0618
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
    /// Returns the <see cref="IMaINHub"/> instance from the zero-config container.
    /// Call <see cref="Initialize"/> first. Use this instead of the <c>[Obsolete]</c> <see cref="AIHub"/>
    /// in script / CLI scenarios where full DI is not set up by the host.
    /// </summary>
    public static IMaINHub Hub =>
        (_serviceProvider ?? throw new AIHubNotInitializedException()).GetRequiredService<IMaINHub>();

    /// <summary>
    /// Initializes the dependency injection container and registers all services.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureSettings">Optional settings configuration.</param>
    public static void Initialize(IConfiguration? configuration = null, Action<MaINSettings>? configureSettings = null)
    {
        // Snapshot any externally-loaded skills (e.g. from AddSkillsFromDirectory) so they
        // survive re-initialization with new settings (e.g. switching backend to OpenAI).
#pragma warning disable CS0618
        var previousSkills = AIHub.GetCurrentSkills()?.ToList();
#pragma warning restore CS0618

        var services = new ServiceCollection();

        if (configuration == null)
        {
            var basePath = Directory.GetCurrentDirectory();
            configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            services.AddLogging(b => b
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = "HH:mm:ss ";
                }));

            services.AddMaIN(configuration, configureSettings);

            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.UseMaIN();

            // Re-register skills that existed before re-init (file-based, user-defined, etc.)
            if (previousSkills is { Count: > 0 })
            {
                var registry = _serviceProvider.GetRequiredService<ISkillRegistry>();
                foreach (var skill in previousSkills)
                {
                    registry.Register(skill);
                }
            }

            Console.WriteLine("AIHub Initialized Successfully");
        }
    }
}
