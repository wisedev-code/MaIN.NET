using MaIN.Domain.Configuration;
using MaIN.Infrastructure;
using MaIN.Services.Configuration;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureMaIN(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<MaINSettings>(configuration.GetSection(MainSectionName));
        
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<INotificationService, NotificationService>();
        serviceCollection.AddSingleton<IAgentFlowService, AgentFlowService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<ILLMService, LLMService>();
        serviceCollection.AddSingleton<IImageGenService, ImageGenService>();
        
        
        var remoteServerUrl = configuration.GetValue<string>("MaIN:RemoteServerUrl");
        if (remoteServerUrl is not null)
        {
            serviceCollection.AddHttpClient<ILLMService, RemoteLLMService>(client =>
            {
                client.BaseAddress = new Uri(remoteServerUrl);
            });
        }
        
        // Register all step handlers
        serviceCollection.AddSingleton<IStepHandler, RedirectStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, FetchDataStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, AnswerStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, BecomeStepHandler>();
        serviceCollection.AddSingleton<IStepHandler, CleanupStepHandler>();
        
        // Register the step processor
        serviceCollection.AddSingleton<IStepProcessor, StepProcessor>();

        // Register the infrastructure
        serviceCollection.ConfigureInfrastructure(configuration);
        
        return serviceCollection;
    }

    private const string MainSectionName = "MaIN";
}
