using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class XaiExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.Xai;
            options.XaiKey = "<YOUR_XAI_KEY>";
        });
    }
} 