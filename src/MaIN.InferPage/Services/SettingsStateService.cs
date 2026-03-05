namespace MaIN.InferPage.Services;

/// <summary>
/// Scoped service for cross-component settings event communication.
/// NavBar (interactive) fires events, Home (interactive) subscribes.
/// </summary>
public class SettingsStateService
{
    public event Action? OnSettingsRequested;
    public event Action? OnSettingsApplied;

    public void RequestSettings()
    {
        OnSettingsRequested?.Invoke();
    }

    public void NotifySettingsApplied()
    {
        OnSettingsApplied?.Invoke();
    }
}