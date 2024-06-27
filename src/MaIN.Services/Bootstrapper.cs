using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Services;

public static class Bootstrapper
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IChatService, ChatService>();
        serviceCollection.AddScoped<ITranslatorService, TranslatorService>();

        return serviceCollection;
    }
}