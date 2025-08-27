namespace MaIN.Services.Services.Abstract;

public interface ITTSEnginge : IDisposable
{
    void InitializeSession(string modelPath);

    Task<float[]> GenerateAudioAsync(string text, string voiceName = "af_heart", float speed = 1.0f, float pitch = 1.0f);

    void SaveAudioToWav(float[] audioData, string outputPath, int sampleRate = 24000);
    void PlayAudio(float[] audioData, int sampleRate = 24000);
}