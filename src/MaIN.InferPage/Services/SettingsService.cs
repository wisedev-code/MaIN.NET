using Microsoft.JSInterop;

namespace MaIN.InferPage.Services;

public class SettingsService(IJSRuntime js)
{
    private const string SettingsKey = "inferpage-settings";
    private const string ApiKeysKey = "inferpage-apikeys";
    private const string ModelHistoryKey = "inferpage-model-history";

    public async Task<InferPageSettings?> LoadSettingsAsync()
        => await js.InvokeAsync<InferPageSettings?>("settingsManager.load", SettingsKey);

    public async Task SaveSettingsAsync(InferPageSettings settings)
        => await js.InvokeVoidAsync("settingsManager.save", SettingsKey, settings);

    public async Task<bool> HasSettingsAsync()
        => await js.InvokeAsync<bool>("settingsManager.exists", SettingsKey);

    public Task SaveApiKeyAsync(string backend, string key) => SetInDictAsync(ApiKeysKey, backend, key);
    public Task<string?> GetApiKeyForBackendAsync(string backend) => GetFromDictAsync(ApiKeysKey, backend);

    public Task SaveModelForBackendAsync(string backend, string model) => SetInDictAsync(ModelHistoryKey, backend, model);
    public Task<string?> GetLastModelForBackendAsync(string backend) => GetFromDictAsync(ModelHistoryKey, backend);

    private const string BackendProfilesKey = "inferpage-backend-profiles";

    public async Task SaveProfileForBackendAsync(string backend, string model,
        bool vision, bool reasoning, bool imageGen)
    {
        var profiles = await js.InvokeAsync<Dictionary<string, BackendProfile>?>(
            "settingsManager.load", BackendProfilesKey) ?? new();
        profiles[backend] = new BackendProfile(model, vision, reasoning, imageGen);
        await js.InvokeVoidAsync("settingsManager.save", BackendProfilesKey, profiles);
    }

    public async Task<BackendProfile?> GetProfileForBackendAsync(string backend)
    {
        var profiles = await js.InvokeAsync<Dictionary<string, BackendProfile>?>(
            "settingsManager.load", BackendProfilesKey);
        return profiles?.GetValueOrDefault(backend);
    }

    private async Task SetInDictAsync(string storageKey, string key, string value)
    {
        var dict = await LoadDictAsync(storageKey);
        dict[key] = value;
        await js.InvokeVoidAsync("settingsManager.save", storageKey, dict);
    }

    private async Task<string?> GetFromDictAsync(string storageKey, string key)
        => (await LoadDictAsync(storageKey)).GetValueOrDefault(key);

    private async Task<Dictionary<string, string>> LoadDictAsync(string storageKey)
        => await js.InvokeAsync<Dictionary<string, string>?>("settingsManager.load", storageKey) ?? new();
}

public record BackendProfile(string Model, bool Vision, bool Reasoning, bool ImageGen);
