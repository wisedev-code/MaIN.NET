using MaIN.Services;
using MaIN.Services.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace MaIN.Core;

public static class Bootstrapper
{
    public static IServiceCollection AddMaIN(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ConfigureMaIN(configuration);
        return services;
    }

    public static IServiceProvider UseMaINAgentFramework(IServiceProvider services)
    {
        services.InitializeAgents();
        return services;
    }

    public static WebApplication UseMaINAgentFramework(this WebApplication app)
    {
        app.Services.InitializeAgents();
        return app;
    }
}