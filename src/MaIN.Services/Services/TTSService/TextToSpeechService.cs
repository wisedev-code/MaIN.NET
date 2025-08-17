using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.Models;
using NAudio.Wave;

namespace MaIN.Services.Services.TTSService;

public interface ITextToSpeechService
{
    Task<ChatResult?> Send(ChatResult result, string modelName, Voice voice, bool playback);
}

public class TextToSpeechService : ITextToSpeechService
{
    private readonly MaINSettings options;

    public TextToSpeechService(MaINSettings options)
    {
        this.options = options;
        VoiceService.SetVoicesPath(options.VoicesPath ?? "voices");
    }

    public async Task<ChatResult?> Send(ChatResult result, string modelName, Voice voice, bool playback)
    {
        var model = KnownModels.GetModel(modelName);
        var audioData = GenerateTtsAudio(Path.Combine(GetModelsPath(), model.FileName), voice, result.Message.Content);
        
        result.SpeechBytes = audioData;

        if (playback)
        {
            await PlaybackAudio(audioData);
        }

        return result;
    }
    
    private static byte[] GenerateTtsAudio(string modelPath, Voice voice, string message, float speed = 1.0f)
    {
        using var model = new TextToSpeechModel(modelPath);
        
        var tokens = Tokenizer.Tokenize(message);
        var audioSamples = model.Infer(tokens, voice.Features, speed);
        
        return ConvertToWav(audioSamples);
    }

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

    private async Task PlaybackAudio(byte[] audioData)
    {
        using var ms = new MemoryStream(audioData);
        using var reader = new WaveFileReader(ms);
        using var output = new WaveOutEvent();
        
        output.Init(reader);
        output.Play();

        //TODO: do better playback end detection
        while (output.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100);
        }
    }

    private string GetModelsPath()
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable("MaIN_ModelsPath");
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Models path not found in configuration or environment variables");
        }

        return path;
    }
}