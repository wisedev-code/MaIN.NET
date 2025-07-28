using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.TTSService;

public interface ITTSService
{
    Task<ChatResult?> Send(Chat chat);
}

public class TTSService : ITTSService
{
    private ITTSEnginge _enginge;
    
    public TTSService()
    {
    }

    public async Task<ChatResult?> Send(Chat chat)
    {
        var audioData = StandaloneTTS.GenerateTTSAudio(chat.TTSModelPath, chat.TTSVoicePath, chat.Messages.Last().Content);
        
        await File.WriteAllBytesAsync($@"C:\Models\tts\output_{DateTime.Now:yyyyMMdd_HHmmss}.wav", audioData);

        
        //_enginge = new TTSEnginge(chat.TTSModelPath, chat.TTSVoicePath);
            
        //var audioData = await _enginge.GenerateAudioAsync(chat.Messages.Last().Content, chat.TTSVoicePath);

        //var outputPath = $@"C:\Models\tts\output_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        
        //_enginge.SaveAudioToWav(audioData, outputPath);
        
        //_enginge.PlayAudio(audioData);
            
        // var tts = KokoroTTS.LoadModel(chat.TTSModelPath);
        // var voice = KokoroVoiceManager.GetVoice(chat.TTSVoice);
        //
        // var semaphore = new SemaphoreSlim(0, 1);
        //
        // tts.OnSpeechCompleted += _ =>
        // {
        //     semaphore.Release();
        // };
        //
        // tts.Speak(chat.Messages.Last().Content, voice);
        //
        // await semaphore.WaitAsync();
        //
        // chat.Messages.Last().MarkProcessed();
        //
        // return new ChatResult
        // {
        //     Done = true,
        //     CreatedAt = DateTime.Now,
        //     Model = chat.Model,
        //     Message = chat.Messages.Last()
        // };

        return null;
    }
}