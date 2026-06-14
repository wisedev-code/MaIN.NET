using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts.Interfaces.ChatContext;
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
    public void AddMaIN_registers_IMaINHub_as_Singleton()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddMaIN(configuration);

        var descriptor = services.Single(d => d.ServiceType == typeof(IMaINHub));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void IMaINHub_Chat_returns_IChatBuilderEntryPoint()
    {
        var provider = BuildProvider();

        using var scope = provider.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IMaINHub>();
        var context = hub.Chat();

        Assert.NotNull(context);
        Assert.IsAssignableFrom<IChatBuilderEntryPoint>(context);
    }

    [Fact]
    public void Two_scopes_get_same_hub_instance()
    {
        var provider = BuildProvider();

        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var hubA = scopeA.ServiceProvider.GetRequiredService<IMaINHub>();
        var hubB = scopeB.ServiceProvider.GetRequiredService<IMaINHub>();

        Assert.Same(hubA, hubB);
    }
}
