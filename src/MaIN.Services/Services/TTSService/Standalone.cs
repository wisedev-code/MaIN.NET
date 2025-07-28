using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MaIN.Services.Services.TTSService;

public static class StandaloneTTS
{
    public static byte[] GenerateTTSAudio(string modelPath, string voicePath, string message, float speed = 1.0f)
    {
        // Load the voice
        var voice = LoadVoice(voicePath);
        
        // Load the model
        using var model = new KokoroModel(modelPath);
        
        // Tokenize the text (simplified version - you'll need a tokenization solution)
        var tokens = TokenizeText(message);
        
        // Generate audio samples
        var audioSamples = model.Infer(tokens, voice, speed);
        
        // Convert to WAV byte array
        return ConvertToWav(audioSamples);
    }
    
    private static float[,,] LoadVoice(string voicePath)
    {
        // This uses NumSharp like the original - you may need to replace with your preferred tensor library
        // For example, you could use System.Text.Json to save/load as JSON, or another serialization method
        return NumSharp.np.Load<float[,,]>(voicePath);
    }
    
    private static int[] TokenizeText(string text)
    {
        return Tokenizer.Tokenize(text);
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
}

// Extracted and simplified KokoroModel class
public sealed class KokoroModel : IDisposable
{
    private readonly InferenceSession session;
    private readonly SessionOptions defaultOptions = new() 
    { 
        EnableMemoryPattern = true, 
        InterOpNumThreads = 8, 
        IntraOpNumThreads = 8 
    };

    public const int MaxTokens = 510;

    public KokoroModel(string modelPath, SessionOptions options = null)
    {
        session = new InferenceSession(modelPath, options ?? defaultOptions);
    }

    public float[] Infer(int[] tokens, float[,,] voiceStyle, float speed = 1)
    {
        var (B, T, C) = (1, tokens.Length, voiceStyle.GetLength(2));
        
        if (tokens.Length == 0)
            return Array.Empty<float>();
            
        if (tokens.Length > MaxTokens)
        {
            Array.Resize(ref tokens, T = MaxTokens);
        }

        var tokenTensor = new DenseTensor<long>(new[] { B, T + 2 });
        var styleTensor = new DenseTensor<float>(new[] { B, C });
        var speedTensor = new DenseTensor<float>(new[] { speed }, new[] { B });

        // Form Kokoro's input (<start>{text}<end>)
        var inputTokens = new int[T + 2];
        Array.Copy(tokens, 0, inputTokens, 1, T);

        for (int j = 0; j < C; j++) 
        { 
            styleTensor[0, j] = voiceStyle[T - 1, 0, j]; 
        }
        
        for (int i = 0; i < inputTokens.Length; i++) 
        { 
            tokenTensor[0, i] = inputTokens[i] >= 0 ? inputTokens[i] : 4; // [unk] --> '.'
        }

        var inputs = new List<NamedOnnxValue> 
        { 
            NamedOnnxValue.CreateFromTensor("tokens", tokenTensor),
            NamedOnnxValue.CreateFromTensor("style", styleTensor),
            NamedOnnxValue.CreateFromTensor("speed", speedTensor)
        };

        lock (session)
        {
            using var results = session.Run(inputs);
            return results[0].AsTensor<float>().ToArray();
        }
    }

    public void Dispose()
    {
        lock (session)
        {
            session.Dispose();
        }
    }
}