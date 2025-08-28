using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class AnthropicExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.Anthropic;
            options.AnthropicKey = "<YOUR_ANTHROPIC_KEY>";
        });
    }
}