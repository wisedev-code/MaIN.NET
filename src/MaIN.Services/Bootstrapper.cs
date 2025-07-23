using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Infrastructure;
using MaIN.Services.Constants;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps;
using MaIN.Services.Services.Steps.Commands;
using MaIN.Services.Services.TTSService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureMaIN(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<MaINSettings>? configureSettings = null)
    {
        var settings = configuration.GetSection(MainSectionName).Get<MaINSettings>() ?? new MaINSettings();
        
        configureSettings?.Invoke(settings);
        serviceCollection.AddSingleton(settings);
        
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<INotificationService, NotificationService>();
        serviceCollection.AddSingleton<IAgentFlowService, AgentFlowService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<IMemoryService, MemoryService>();
        serviceCollection.AddSingleton<IMemoryFactory, MemoryFactory>();
        serviceCollection.AddSingleton<ILLMServiceFactory, LLMServiceFactory>();
        serviceCollection.AddSingleton<IImageGenServiceFactory, ImageGenServiceFactory>();
        serviceCollection.AddSingleton<ITTSServiceFactory, TTSServiceFactory>();

// Register all concrete implementations as transient
        serviceCollection.AddTransient<LLMService>();
        serviceCollection.AddTransient<OpenAiService>();
        serviceCollection.AddTransient<ImageGenService>();
        serviceCollection.AddTransient<OpenAiImageGenService>();
        serviceCollection.AddTransient<TTSService>();
        
        // Register all step handlers
        serviceCollection.AddSingleton<IStepHandler, RedirectStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, FetchDataStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, AnswerStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, McpStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, BecomeStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, CleanupStepHandler>();
        serviceCollection.AddCommandHandlers();
        
        //AddHttpClients
        serviceCollection.AddHttpClients();
        
        // Register the step processor
        serviceCollection.AddSingleton<IStepProcessor, StepProcessor>();

        // Register the infrastructure
        serviceCollection.ConfigureInfrastructure(configuration);
        
        return serviceCollection;
    }

    private static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IDataSourceProvider, DataSourceProvider>();
        
        services.AddSingleton<ICommandHandler<StartCommand, Message?>, StartCommandHandler>();
        services.AddSingleton<ICommandHandler<RedirectCommand, Message?>, RedirectCommandHandler>();
        services.AddSingleton<ICommandHandler<FetchCommand, Message?>, FetchCommandHandler>();
        services.AddSingleton<ICommandHandler<AnswerCommand, Message?>, AnswerCommandHandler>();
        services.AddSingleton<ICommandHandler<McpCommand, Message?>, McpCommandHandler>();

        services.AddSingleton<ICommandDispatcher, CommandDispatcher>(provider =>
        {
            var dispatcher = new CommandDispatcher(provider);
            
            dispatcher.RegisterNamedHandler<McpCommand, Message?, McpCommandHandler>("MCP");
            dispatcher.RegisterNamedHandler<StartCommand, Message?, StartCommandHandler>("START");
            dispatcher.RegisterNamedHandler<RedirectCommand, Message?, RedirectCommandHandler>("REDIRECT");
            dispatcher.RegisterNamedHandler<FetchCommand, Message?, FetchCommandHandler>("FETCH_DATA");
            dispatcher.RegisterNamedHandler<FetchCommand, Message?, FetchCommandHandler>("FETCH_DATA*");
            dispatcher.RegisterNamedHandler<AnswerCommand, Message?, AnswerCommandHandler>("ANSWER");
            
            return dispatcher;
        });
        
        services.AddSingleton<StartCommandHandler>();
        services.AddSingleton<RedirectCommandHandler>();
        services.AddSingleton<FetchCommandHandler>();
        services.AddSingleton<AnswerCommandHandler>();
        services.AddSingleton<McpCommandHandler>();

        return services;
    }
    
    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(ServiceConstants.HttpClients.ImageGenClient, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.AddHttpClient(ServiceConstants.HttpClients.OpenAiClient);
        services.AddHttpClient(ServiceConstants.HttpClients.GeminiClient);
        services.AddHttpClient(ServiceConstants.HttpClients.ImageDownloadClient);
        services.AddHttpClient(ServiceConstants.HttpClients.ModelContextDownloadClient, client =>
        {
            client.Timeout = TimeSpan.FromHours(10);
        });
        return services;
    }

    private const string MainSectionName = "MaIN";
}
