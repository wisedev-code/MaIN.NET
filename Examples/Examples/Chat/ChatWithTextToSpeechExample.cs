using MaIN.Core.Hub;

namespace Examples;

public class ChatWithTextToSpeechExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running!");

        var chatResult = await AIHub.Chat().WithModel("gemma2:2b")
            .Speak(@"C:\Models\tts\kokoro.onnx", @"C:\Models\tts\voices\af_nicole.npy")
            .WithMessage("Generate a 4 sentence poem.")
            .CompleteAsync();
        
        await File.WriteAllBytesAsync($@"C:\Models\tts\output_{DateTime.Now:yyyyMMdd_HHmmss}.wav", chatResult.SpeechBytes!);

        Console.WriteLine(chatResult.Message.Content);
        
    }
}