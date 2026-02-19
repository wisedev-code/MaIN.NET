using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatExampleOllama : IExample
{
    public async Task Start()
    {
        OllamaExample.Setup(); // We need to set Ollama backend type and optionally provide Ollama API key
        Console.WriteLine("(Ollama) ChatExample is running!");

        await AIHub.Chat()
            .WithModel<OllamaGemma3_4b>()
            .WithMessage("Write a short poem about the color green.")
            .CompleteAsync(interactive: true);
    }
}