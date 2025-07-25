using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class ClaudeExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.Claude;
            options.ClaudeKey = "<YOUR_CLAUDE_KEY>";
        });
    }
}