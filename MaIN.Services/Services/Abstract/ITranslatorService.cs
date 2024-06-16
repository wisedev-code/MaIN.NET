namespace MaIN.Services.Services.Abstract;

public interface ITranslatorService
{
    Task<string> DetectLanguage(string text);
    Task<string> Translate(string text, string targetLanguage);
}