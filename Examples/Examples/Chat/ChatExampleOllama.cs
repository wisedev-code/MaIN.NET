using Examples.Utils;
using MaIN.Core.Hub;

namespace Examples.Chat;

public class ChatExampleOllama : IExample
{
    public async Task Start()
    {
        OllamaExample.Setup(); //We need to provide Ollama API key
        Console.WriteLine("(Ollama) ChatExample is running!");

        await AIHub.Chat()
            .WithModel("qwen3-next:80b")
            .WithMessage("Write a short poem about the color green.")
            .CompleteAsync(interactive: true);
    }
}