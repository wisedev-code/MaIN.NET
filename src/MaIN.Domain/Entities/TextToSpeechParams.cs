namespace MaIN.Domain.Entities;

public class TextToSpeechParams
{
    public string Model { get; set; }
    public Voice Voice { get;  set; }
    public bool Playback { get; set; }

    public TextToSpeechParams(string model, Voice voice, bool playback = false)
    {
        Model = model;  
        Voice = voice;
        Playback = playback;
    }
}