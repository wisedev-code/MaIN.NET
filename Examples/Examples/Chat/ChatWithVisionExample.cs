using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithVisionExample : IExample
{
    public async Task Start()
    {
        //https://huggingface.co/cjpais/llava-1.6-mistral-7b-gguf - Tried with this model
        Console.WriteLine("ChatExample with vision model is running!");

        var image = await File.ReadAllBytesAsync(
            Path.Combine(AppContext.BaseDirectory, "Files", "gamex.jpg"));

        await AIHub.Chat()
            .WithModel<Llava16Mistral_7b>()
            .WithMessage("What can you see on the image?", image)
            .CompleteAsync(interactive: true);
    }
}