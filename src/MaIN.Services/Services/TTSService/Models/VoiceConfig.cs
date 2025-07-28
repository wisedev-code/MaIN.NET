using MaIN.Services.Services.TTSService.Test;

namespace MaIN.Services.Services.TTSService.Models;

public class VoiceConfig
{
    public string Name { get; set; }
    public float[,,] Features { get; set; }
    public Language Language => Language.AmericanEnglish;
    public Gender Gender => (Gender) Name[1];

    
    public void Export(string filePath) => NumSharp.np.Save(Features, filePath);
    
    public static VoiceConfig FromPath(string filePath) 
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        return new() { Name = name, Features = NumSharp.np.Load<float[,,]>(filePath) };
    }
    
    public static implicit operator float[,,](VoiceConfig voice) => voice.Features;
    public static implicit operator VoiceConfig(float[,,] features) => new() { Name = "", Features = features };
}