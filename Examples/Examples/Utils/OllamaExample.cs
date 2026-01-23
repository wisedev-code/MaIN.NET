using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;

public class OllamaExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.Ollama;
            //options.OllamaKey = "<YOUR_OLLAMA_KEY>"; // set only if you want to use Ollama cloud
        });
    }
}