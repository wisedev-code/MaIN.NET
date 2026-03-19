using MaIN.Core.Hub;
using MaIN.Domain.Models.Abstract;

namespace Examples.Chat;

public class ChatWithCustomModelIdExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithCustomModelId is running!");

        // Register an existing model file under a custom ID with tailored configuration.
        // The same Gemma2-2b.gguf file, but exposed as a named role alias.
        var writingAssistant = new GenericLocalModel(
            "Gemma2-2b.gguf",
            "Writing Assistant",
            "writing-assistant",
            SystemMessage: "You are a creative writing assistant. Always respond with vivid, expressive language."
        );

        ModelRegistry.Register(writingAssistant);

        await AIHub.Chat()
            .WithModel("writing-assistant")
            .EnsureModelDownloaded()
            .WithMessage("Write a one-sentence opening to a mystery story.")
            .CompleteAsync(interactive: true);
    }
}
