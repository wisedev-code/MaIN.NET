using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;

namespace Examples.Chat;

public class ChatWithImageGenGeminiExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with image gen is running! (Gemini)");
        GeminiExample.Setup(); // We need to provide Gemini API key

        var imagenModel = new GenericImageGenerationCloudModel("imagen-3", BackendType.Gemini);
        ModelRegistry.RegisterOrReplace(imagenModel);
        var result = await AIHub.Chat()
            .WithModel(imagenModel.Id)
            .WithMessage("Generate hamster as a astronaut on the moon")
            .CompleteAsync();

        ImagePreview.ShowImage(result.Message.Image);
    }
}
