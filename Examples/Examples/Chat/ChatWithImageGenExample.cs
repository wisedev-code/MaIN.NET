using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Abstract;

namespace Examples.Chat;

public class ChatWithImageGenExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running!");

        var fluxModel = new GenericLocalModel("FLUX.1_Shnell");
        ModelRegistry.RegisterOrReplace(fluxModel);
        var result = await AIHub.Chat()
            .WithModel(fluxModel.Id)
            .WithMessage("Generate cyberpunk godzilla cat warrior")
            .CompleteAsync();

        ImagePreview.ShowImage(result.Message.Image);
    }
}
