using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Infrastructure;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps;
using MaIN.Services.Services.Steps.Commands;
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
        // Load settings from configuration
        var settings = configuration.GetSection(MainSectionName).Get<MaINSettings>() ?? new MaINSettings();
        
        // Apply additional configuration if provided
        configureSettings?.Invoke(settings);

        // Register the updated settings
        serviceCollection.AddSingleton(settings);
        
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<INotificationService, NotificationService>();
        serviceCollection.AddSingleton<IAgentFlowService, AgentFlowService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<ILLMService, LLMService>();
        serviceCollection.AddSingleton<IImageGenService, ImageGenService>();


        if (settings.BackendType == BackendType.OpenAi)
        {
            serviceCollection.AddSingleton<ILLMService, OpenAiService>();
            serviceCollection.AddSingleton<IImageGenService, OpenAiImageGenService>();
        }
        
        // Register all step handlers
        serviceCollection.AddSingleton<IStepHandler, RedirectStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, FetchDataStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, AnswerStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, BecomeStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, CleanupStepHandler>();
        serviceCollection.AddCommandHandlers();
        
        // Register the step processor
        serviceCollection.AddSingleton<IStepProcessor, StepProcessor>();

        // Register the infrastructure
        serviceCollection.ConfigureInfrastructure(configuration);
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IDataSourceProvider, DataSourceProvider>();
        
        services.AddSingleton<ICommandHandler<StartCommand, Message?>, StartCommandHandler>();
        services.AddSingleton<ICommandHandler<RedirectCommand, Message?>, RedirectCommandHandler>();
        services.AddSingleton<ICommandHandler<FetchCommand, Message?>, FetchCommandHandler>();
        services.AddSingleton<ICommandHandler<AnswerCommand, Message?>, AnswerCommandHandler>();

        services.AddSingleton<ICommandDispatcher, CommandDispatcher>(provider =>
        {
            var dispatcher = new CommandDispatcher(provider);
            
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

        return services;
    }

    private const string MainSectionName = "MaIN";
}
