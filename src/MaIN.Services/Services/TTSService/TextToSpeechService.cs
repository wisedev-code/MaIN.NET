using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.TTSService;

public interface ITextToSpeechService
{
    Task<ChatResult?> Send(ChatResult result, string ttsModelPath, string ttsVoicePath);
}

public class TextToSpeechService : ITextToSpeechService
{
    public async Task<ChatResult?> Send(ChatResult result, string ttsModelPath, string ttsVoicePath)
    {
        var audioData = GenerateTtsAudio(ttsModelPath, ttsVoicePath, result.Message.Content);
        
        result.SpeechBytes = audioData;

        return result;
    }
    
    private static byte[] GenerateTtsAudio(string modelPath, string voicePath, string message, float speed = 1.0f)
    {
        // Load the voice
        var voice = LoadVoice(voicePath);
        
        // Load the model
        using var model = new TextToSpeechModel(modelPath);
        
        // Tokenize the text (simplified version - you'll need a tokenization solution)
        var tokens = TokenizeText(message);
        
        // Generate audio samples
        var audioSamples = model.Infer(tokens, voice, speed);
        
        // Convert to WAV byte array
        return ConvertToWav(audioSamples);
    }
    
    private static float[,,] LoadVoice(string voicePath) => NumSharp.np.Load<float[,,]>(voicePath);

    private static int[] TokenizeText(string text) => Tokenizer.Tokenize(text);

    private static byte[] ConvertToWav(float[] samples)
    {
        const int sampleRate = 24000; // Kokoro's output sample rate
        const int bitsPerSample = 16;
        const int channels = 1;
        
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // WAV header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + samples.Length * 2); // File size
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // Format chunk size
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
        writer.Write((short)(channels * bitsPerSample / 8)); // Block align
        writer.Write((short)bitsPerSample);
        writer.Write("data".ToCharArray());
        writer.Write(samples.Length * 2); // Data size
        
        // Convert float samples to 16-bit PCM
        foreach (var sample in samples)
        {
            var pcmSample = (short)(Math.Max(-1.0f, Math.Min(1.0f, sample)) * 32767);
            writer.Write(pcmSample);
        }
        
        return ms.ToArray();
    }
}