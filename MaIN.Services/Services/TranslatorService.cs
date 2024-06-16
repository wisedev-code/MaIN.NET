using GTranslate.Translators;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class TranslatorService : ITranslatorService
{
    private readonly AggregateTranslator _translator = new();

    public async Task<string> DetectLanguage(string text)
         => (await _translator.DetectLanguageAsync(text)).Name;
    

    public async Task<string> Translate(string text, string targetLanguage)
        => (await _translator.TranslateAsync(text, targetLanguage)).Translation;
}