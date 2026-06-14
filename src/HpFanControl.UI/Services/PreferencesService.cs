using System.Text.Json;
using HpFanControl.UI.Models;

namespace HpFanControl.UI.Services;

#pragma warning disable CA1812
internal sealed class PreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly string _filePath;

    public PreferencesService()
    {
        string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hp-fan-control");
        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "preferences.json");
    }

    public async Task<AppPreferences> GetPreferencesAsync()
    {
        if (!File.Exists(_filePath))
            return new AppPreferences();

        string json = await File.ReadAllTextAsync(_filePath).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppPreferences>(json, JsonOptions) ?? new AppPreferences();
    }

    public async Task SavePreferencesAsync(AppPreferences prefs)
    {
        string json = JsonSerializer.Serialize(prefs, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json).ConfigureAwait(false);
    }
}