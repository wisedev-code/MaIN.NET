using KokoroSharp;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.TTSService;

public interface ITTSService
{
    Task<ChatResult?> Send(Chat chat);
}

public class TTSService : ITTSService
{
    public TTSService()
    {
    }

    public async Task<ChatResult?> Send(Chat chat)
    {
        var model = KnownModels.GetModel(chat.Model);
        var tts = KokoroTTS.LoadModel(model.Path);
        var voice = KokoroVoiceManager.GetVoice(chat.Voice);
        
        var semaphore = new SemaphoreSlim(0, 1);

        tts.OnSpeechCompleted += _ =>
        {
            semaphore.Release();
        };

        tts.Speak(chat.Messages.Last().Content, voice);

        await semaphore.WaitAsync();

        chat.Messages.Last().MarkProcessed();
        
        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = chat.Messages.Last()
        };
    }
}