using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaIN.Core.IntegrationTests;

public class IntegrationTestBase : IDisposable
{
    protected readonly IHost _host;
    protected readonly IServiceProvider _services;

    protected IntegrationTestBase()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddMaIN(context.Configuration);
                ConfigureServices(services);
            })
            .Build();

        _host.Services.UseMaIN();
        _host.Start();

        _services = _host.Services;
    }

    // Allow derived classes to add additional services or override existing ones
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected T GetService<T>() where T : notnull
    {
        return _services.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}