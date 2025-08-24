using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using MaIN.Services.Services.TTSService;

namespace Examples;

public class ChatWithTextToSpeechExample : IExample
{
    private const string VoicePath = @"C:\Models\tts\voices";
    
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running! Put on your headphones and press any key.");
        Console.ReadKey();
        
        VoiceService.SetVoicesPath(VoicePath);
        var voice = VoiceService.GetVoice("af_heart")
            .MixWith(VoiceService.GetVoice("bf_emma"));
        
        await AIHub.Chat().WithModel("gemma2:2b")
            .WithMessage("Generate a 4 sentence poem.")
            .Speak(new TextToSpeechParams("kokoro:82m", voice, true))
            .CompleteAsync(interactive: true);

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}