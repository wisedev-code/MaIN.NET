using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class GeminiExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.Gemini;
            options.GeminiKey = "<Gemini API Key>";
        });
    }
}