using MaIN.Core.Hub;

namespace Examples;

public class ChatWithTextToSpeechExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatWithTextToSpeech is running! Put on your headphones and press any key.");
        Console.ReadKey();

        const string modelPath = @"C:\Models\tts\kokoro.onnx";
        const string voicePath = @"C:\Models\tts\voices\af_nicole.npy";

        await AIHub.Chat().WithModel("gemma2:2b")
            .Speak(modelPath, voicePath, playback: true)
            .WithMessage("Generate a 4 sentence poem.")
            .CompleteAsync(interactive: true);

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}