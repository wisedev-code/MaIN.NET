using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts;
using MaIN.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaIN.Core.UnitTests;

public class MaINHubRegistrationTests
{
    private static IServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMaIN(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddMaIN_registers_IMaINHub_as_Scoped()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddMaIN(configuration);

        var descriptor = services.Single(d => d.ServiceType == typeof(IMaINHub));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void IMaINHub_Chat_returns_ChatContext()
    {
        var provider = BuildProvider();

        using var scope = provider.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IMaINHub>();
        var context = hub.Chat();

        Assert.NotNull(context);
        Assert.IsType<ChatContext>(context);
    }

    [Fact]
    public void Two_scopes_get_distinct_hub_instances_but_shared_services()
    {
        var provider = BuildProvider();

        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var hubA = scopeA.ServiceProvider.GetRequiredService<IMaINHub>();
        var hubB = scopeB.ServiceProvider.GetRequiredService<IMaINHub>();
        var servicesA = scopeA.ServiceProvider.GetRequiredService<IAIHubServices>();
        var servicesB = scopeB.ServiceProvider.GetRequiredService<IAIHubServices>();

        Assert.NotSame(hubA, hubB);
        Assert.Same(servicesA, servicesB);
    }
}
