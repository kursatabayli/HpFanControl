namespace HpFanControl.UI.Models;

internal sealed class AppPreferences
{
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public string SkippedVersion { get; set; } = string.Empty;
}
