using System.Reflection;
using System.Text.Json;
using HpFanControl.Core.Helpers;
using HpFanControl.UI.Models;
using Microsoft.Extensions.Logging;

namespace HpFanControl.UI.Services;

#pragma warning disable CA1812
internal sealed partial class PreferencesService
{
    private const string PrefsFileName = "preferences.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly ILogger<PreferencesService> _logger;
    private readonly string _configDir;
    private readonly string _filePath;

    public PreferencesService(ILogger<PreferencesService> logger)
    {
        _logger = logger;

        _configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.Name);
        _filePath = Path.Combine(_configDir, PrefsFileName);

        EnsureDirectoryExists();
    }

    public async Task<AppPreferences> GetPreferencesAsync()
    {
        if (!File.Exists(_filePath))
            return new AppPreferences();

        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<AppPreferences>(stream, JsonOptions).ConfigureAwait(false)
                   ?? new AppPreferences();
        }
        catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
        {
            LogLoadError(ex);
            return new AppPreferences();
        }
    }

    public async Task SavePreferencesAsync(AppPreferences prefs)
    {
        try
        {
            EnsureDirectoryExists();

            using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, prefs, JsonOptions).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
        {
            LogSaveError(ex);
        }
    }

    private void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            LogDirectoryError(ex);
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to load preferences. Using defaults.")]
    private partial void LogLoadError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to save preferences.")]
    private partial void LogSaveError(Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to create preferences directory.")]
    private partial void LogDirectoryError(Exception ex);
    #endregion
}