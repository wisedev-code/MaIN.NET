using System.Net.Sockets;
using MaIN.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaIN.Core.IntegrationTests;

public class IntegrationTestBase : IDisposable
{
    protected readonly IHost _host;
    protected readonly IServiceProvider _services;

    public IntegrationTestBase()
    {
        _host = CreateHost();
        _host.Start();
        
        _services = _host.Services;
    }

    private IHost CreateHost()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseUrls("http://localhost:0") // Random available port
                    .ConfigureServices((context, services) =>
                    {
                        services.AddMaIN(context.Configuration);
                        
                        var provider = services.BuildServiceProvider();
                        provider.UseMaIN();
                    });
            });

        return hostBuilder.Build();
    }

    protected T GetService<T>() where T : notnull
    {
        return _services.GetRequiredService<T>();
    }

    protected static bool PingHost(string host, int port, int timeout)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
            
                if (!success)
                {
                    return false;
                }
            
                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}