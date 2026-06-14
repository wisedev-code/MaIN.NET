using MaIN.Core.Hub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaIN.Core.IntegrationTests;

public class IntegrationTestBase : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    protected IMaINHub AIHub { get; }

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
        AIHub = _services.GetRequiredService<IMaINHub>();
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
