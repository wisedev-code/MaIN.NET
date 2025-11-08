using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MaIN.Services.Services.TTSService;

/// <summary>
/// Currently working with kokoro model. WIP to work with other
/// </summary>
public sealed class TextToSpeechModel : IDisposable
{
    private readonly InferenceSession _session;
    private readonly SessionOptions _defaultOptions = new() 
    { 
        EnableMemoryPattern = true, 
        InterOpNumThreads = 8, 
        IntraOpNumThreads = 8 
    };

    public const int MaxTokens = 510;

    public TextToSpeechModel(string modelPath, SessionOptions? options = null)
    {
        _session = new InferenceSession(modelPath, options ?? _defaultOptions);
    }

    public float[] Infer(int[] tokens, float[,,] voiceStyle, float speed = 1)
    {
        var (B, T, C) = (1, tokens.Length, voiceStyle.GetLength(2));
        
        switch (tokens.Length)
        {
            case 0:
                return [];
            case > MaxTokens:
                Array.Resize(ref tokens, T = MaxTokens);
                break;
        }

        var tokenTensor = new DenseTensor<long>(new[] { B, T + 2 });
        var styleTensor = new DenseTensor<float>(new[] { B, C });
        var speedTensor = new DenseTensor<float>(new[] { speed }, new[] { B });

        // Form Kokoro's input (<start>{text}<end>)
        var inputTokens = new int[T + 2];
        Array.Copy(tokens, 0, inputTokens, 1, T);

        for (var j = 0; j < C; j++) 
        { 
            styleTensor[0, j] = voiceStyle[T - 1, 0, j]; 
        }
        
        for (var i = 0; i < inputTokens.Length; i++) 
        { 
            tokenTensor[0, i] = inputTokens[i] >= 0 ? inputTokens[i] : 4; // [unk] --> '.'
        }

        var inputs = new List<NamedOnnxValue> 
        { 
            NamedOnnxValue.CreateFromTensor("tokens", tokenTensor),
            NamedOnnxValue.CreateFromTensor("style", styleTensor),
            NamedOnnxValue.CreateFromTensor("speed", speedTensor)
        };

        lock (_session)
        {
            using var results = _session.Run(inputs);
            return results[0].AsTensor<float>().ToArray();
        }
    }

    public void Dispose()
    {
        lock (_session)
        {
            _session.Dispose();
        }
    }
}