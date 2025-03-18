using System.Net.Sockets;
using MaIN.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Core.IntegrationTests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddMaIN(context.Configuration);
                
                var provider = services.BuildServiceProvider();
                provider.UseMaIN();
            });
        });
        
        _client = _factory.CreateClient();
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
}