namespace MaIN.InferPage.Services;

/// <summary>Event bus for NavBar ↔ Home sibling communication.</summary>
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