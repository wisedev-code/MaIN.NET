using MaIN.Core;
using MaIN.Domain.Configuration;

namespace Examples.Utils;


public class OpenAiExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: (options) =>
        {
            options.BackendType = BackendType.OpenAi;
           // options.OpenAiKey = "<YOUR_OPENAI_KEY>";
        });
    }
}