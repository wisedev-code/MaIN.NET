using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Services.TTSService;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Examples.Chat;

public class ChatWithTextToSpeechExample : IExample
{
    private const string VoicePath = "<your-path-to-voices>";
    
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running! Put on your headphones and press any key.");
        Console.ReadKey();
        
        VoiceService.SetVoicesPath(VoicePath);
        var voice = VoiceService.GetVoice("af_heart")
            .MixWith(VoiceService.GetVoice("bf_emma"));
        
        await AIHub.Chat()
            .WithModel<Gemma2_2b>()
            .WithMessage("Generate a 4 sentence poem.")
            .Speak(new TextToSpeechParams(new Kokoro_82m(), voice, true))
            .CompleteAsync(interactive: true);

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}