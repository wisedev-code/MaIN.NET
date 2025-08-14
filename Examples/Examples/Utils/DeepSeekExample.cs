using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class DeepSeekExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.DeepSeek;
            options.DeepSeekKey = "<YOUR_DEEPSEEK_KEY>";
        });
    }
}