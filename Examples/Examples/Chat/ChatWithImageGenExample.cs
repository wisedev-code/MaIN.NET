using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Abstract;

namespace Examples.Chat;

public class ChatWithImageGenExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running!");

        ModelRegistry.RegisterOrReplace(new GenericLocalModel(Models.Local.Flux1Shnell));
        var result = await AIHub.Chat()
            .WithModel(Models.Local.Flux1Shnell)
            .WithMessage("Generate cyberpunk godzilla cat warrior")
            .CompleteAsync();

        ImagePreview.ShowImage(result.Message.Image);
    }
}
