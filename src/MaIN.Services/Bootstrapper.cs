using MaIN.Domain.Configuration;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureMaIN(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<MainSettings>(configuration.GetSection(MainSectionName));

        //TODO solve this with separate registration for actions purposes
        serviceCollection.AddSingleton<IImageGenService, ImageGenService>();
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<INotificationService, NotificationService>();
        serviceCollection.AddSingleton<IAgentFlowService, AgentFlowService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<IOllamaService, OllamaService>();

        return serviceCollection;
    }

    private const string MainSectionName = "MaIN";
}