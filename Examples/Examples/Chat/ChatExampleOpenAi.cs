using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Models;

namespace Examples.Chat;

public class ChatExampleOpenAi : IExample
{
    public async Task Start()
    {
        OpenAiExample.Setup(); //We need to provide OpenAi API key

        Console.WriteLine("(OpenAi) ChatExample is running!");

        await AIHub.Chat()
            .WithModel(Models.OpenAi.Gpt5Nano)
            .WithMessage("What do you consider to be the greatest invention in history?")
            .WithInferenceParams(new OpenAiInferenceParams // We could override some inference params
            {
                ResponseFormat = "text",
                AdditionalParams = new Dictionary<string, object>
                {
                    ["max_completion_tokens"] = 2137
                }
            })
            .CompleteAsync(interactive: true);
    }
}
