using MaIN.Core.Hub;
using MaIN.Services.Services.TTSService;

namespace Examples;

public class ChatWithTextToSpeechExample : IExample
{
    private const string VoicePath = "<set_path_to_your_voice_files>";
    
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running! Put on your headphones and press any key.");
        Console.ReadKey();
        
        VoiceService.SetVoicesPath(VoicePath);
        var voice = VoiceService.GetVoice("af_heart")
            .MixWith(VoiceService.GetVoice("bf_emma"));
        
        await AIHub.Chat().WithModel("gemma2:2b")
            .WithMessage("Generate a 4 sentence poem.")
            .Speak("kokoro:82m", voice, true)
            .CompleteAsync(interactive: true);

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}