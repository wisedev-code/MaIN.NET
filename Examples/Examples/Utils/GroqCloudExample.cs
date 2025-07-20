using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class GroqCloudExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.GroqCloud;
            options.DeepSeekKey = "<YOUR_GROQ_KEY>";
        });
    }
}