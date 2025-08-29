using MaIN.Domain.Entities;
using NumSharp;
using Razorvine.Pickle;
using TorchSharp;

namespace MaIN.Services.Services.TTSService;

[Obsolete("This is temporary, duct-tape like solution. It can and will evolve into something more robust")]
public static class VoiceService
{
    private static string? _voicesPath;
    
    public static void SetVoicesPath(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException();
        }
        _voicesPath = path;
    }
    
    public static Voice GetVoice(string name)
    {
        if(string.IsNullOrEmpty(_voicesPath))
            throw new Exception("Voices path is not set");

        var voicePath = Path.Combine(_voicesPath, name);

        var features = Path.GetExtension(name) switch
        {
            ".npy" => NumSharp.np.Load<float[,,]>(voicePath),
            ".bin" => LoadFromBinary(voicePath, (510, 1, 256)),
            _ => throw new Exception("Unsupported voice file extension")
        };

        if (!File.Exists(voicePath))
            throw new FileNotFoundException("Voice file cannot be found");

        return new Voice
        {
            Name = Path.GetFileNameWithoutExtension(name),
            Features = features
        };
    }

    /// <summary>
    /// Once again thanks KokoroSharp for this wonderful piece of code
    /// </summary>
    public static Voice Mix(params (Voice voice, float weight)[] voices)
    {
        var f = voices[0].voice.Features;
        
        var (w, h, d) = (f.GetLength(0), f.GetLength(1), f.GetLength(2));

        var summedWeights = voices.Sum(x => x.weight);
        var normedWeights = voices.Select(x => x.weight / summedWeights).ToArray();

        var newArray = np.zeros_like(voices[0].voice.Features);
        for (var i = 0; i < voices.Length; i++)
        {
            newArray += np.array(voices[i].voice.Features) * normedWeights[i];
        }
        
        var newFeatures = newArray.reshape(new Shape(w, h, d)).ToMuliDimArray<float>() as float[,,];
        
        var name = (voices[0].voice.Name.Length >= 3) ? $"{voices[0].voice.Name[..2]}_mix" : "am_mix";

        return new Voice
        {
            Name = name,
            Features = newFeatures
        };
    }
    
    public static Voice MixWith(this Voice baseVoice, Voice mixWithVoce, float weightBase = 0.5f, float weightMixWith = 0.5f)
        => Mix((baseVoice, weightBase), (mixWithVoce, weightMixWith));
    
    private static float[,,] LoadFromBinary(string path, (int width, int height, int depth) dimensions)
    {
        var bytes = File.ReadAllBytes(path);
        var result = new float[dimensions.width, dimensions.height, dimensions.depth];
        
        var index = 0;
        for (var x = 0; x < dimensions.width; x++)
        {
            for (var y = 0; y < dimensions.height; y++)
            {
                for (var z = 0; z < dimensions.depth; z++)
                {
                    result[x, y, z] = BitConverter.ToSingle(bytes, index);
                    index += 4;
                }
            }
        }
        
        return result;
    }
}