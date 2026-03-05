using Microsoft.JSInterop;

namespace MaIN.InferPage.Services;

public class SettingsService
{
    private const string SettingsKey = "inferpage-settings";
    private const string ApiKeysKey = "inferpage-apikeys";
    private const string ModelHistoryKey = "inferpage-model-history";

    private readonly IJSRuntime _js;

    public SettingsService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<InferPageSettings?> LoadSettingsAsync()
    {
        return await _js.InvokeAsync<InferPageSettings?>("settingsManager.load", SettingsKey);
    }

    public async Task SaveSettingsAsync(InferPageSettings settings)
    {
        await _js.InvokeVoidAsync("settingsManager.save", SettingsKey, settings);
    }

    public async Task<bool> HasSettingsAsync()
    {
        return await _js.InvokeAsync<bool>("settingsManager.exists", SettingsKey);
    }

    public async Task<Dictionary<string, string>?> LoadApiKeysAsync()
    {
        return await _js.InvokeAsync<Dictionary<string, string>?>("settingsManager.load", ApiKeysKey);
    }

    public async Task SaveApiKeyAsync(string backendName, string key)
    {
        var keys = await LoadApiKeysAsync() ?? new Dictionary<string, string>();
        keys[backendName] = key;
        await _js.InvokeVoidAsync("settingsManager.save", ApiKeysKey, keys);
    }

    public async Task<string?> GetApiKeyForBackendAsync(string backendName)
    {
        var keys = await LoadApiKeysAsync();
        return keys?.GetValueOrDefault(backendName);
    }

    public async Task<Dictionary<string, string>?> LoadModelHistoryAsync()
    {
        return await _js.InvokeAsync<Dictionary<string, string>?>("settingsManager.load", ModelHistoryKey);
    }

    public async Task SaveModelForBackendAsync(string backendName, string model)
    {
        var history = await LoadModelHistoryAsync() ?? new Dictionary<string, string>();
        history[backendName] = model;
        await _js.InvokeVoidAsync("settingsManager.save", ModelHistoryKey, history);
    }

    public async Task<string?> GetLastModelForBackendAsync(string backendName)
    {
        var history = await LoadModelHistoryAsync();
        return history?.GetValueOrDefault(backendName);
    }
}