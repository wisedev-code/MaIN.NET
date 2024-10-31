using MaIN.Domain.Configuration;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureMaIN(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<MainSettings>(configuration.GetSection(MainSectionName));
        
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<INotificationService, NotificationService>();
        serviceCollection.AddSingleton<IAgentFlowService, AgentFlowService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<IOllamaService, OllamaService>();
        serviceCollection.AddSingleton<IImageGenService, ImageGenService>();
        
        // Register all step handlers
        serviceCollection.AddSingleton<IStepHandler, RedirectStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, FetchDataStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, AnswerStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, BecomeStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, CleanupStepHandler>();
        
        // Register the step processor
        serviceCollection.AddSingleton<IStepProcessor, StepProcessor>();
        
        return serviceCollection;
    }

    private const string MainSectionName = "MaIN";
}