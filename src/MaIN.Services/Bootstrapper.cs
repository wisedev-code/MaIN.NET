using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection serviceCollection)
    {
        //TODO solve this with separate registration for actions purposes
        serviceCollection.AddSingleton<IChatService, ChatService>();
        serviceCollection.AddSingleton<IAgentService, AgentService>();
        serviceCollection.AddSingleton<ITranslatorService, TranslatorService>();
        serviceCollection.AddSingleton<IOllamaService, OllamaService>();

        return serviceCollection;
    }
}