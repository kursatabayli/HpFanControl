using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.Core.Services.Implementations;

public sealed partial class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configFolder;
    private readonly string _configPath;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;

        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configFolder = Path.Combine(baseDir, "hp-fan-control");
        _configPath = Path.Combine(_configFolder, "config.json");
    }

    public FanConfig Load()
    {
        if (!File.Exists(_configPath))
            return EnsureConfigFileCreated();

        try
        {
            using var stream = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var config = JsonSerializer.Deserialize(stream, AppJsonContext.Default.FanConfig);

            if (config != null)
            {
                config.ValidateAndSortCurves();
                return config;
            }

            return EnsureConfigFileCreated();
        }
        catch (JsonException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
        catch (IOException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
        catch (UnauthorizedAccessException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
    }

    public void Save(FanConfig config)
    {
        try
        {
            if (!Directory.Exists(_configFolder))
                Directory.CreateDirectory(_configFolder);

            string tmpPath = _configPath + ".tmp";

            using (var stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, config, AppJsonContext.Default.FanConfig);
                stream.Flush();
            }

            File.Move(tmpPath, _configPath, overwrite: true);

            LogSaveSuccess();
        }
        catch (JsonException ex)
        {
            LogSaveError(ex);
        }
        catch (IOException ex)
        {
            LogSaveError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogSaveError(ex);
        }
    }

    private FanConfig EnsureConfigFileCreated()
    {
        try
        {
            if (!Directory.Exists(_configFolder))
                Directory.CreateDirectory(_configFolder);

            var defaultConfig = FanConfig.Default;
            Save(defaultConfig);
            return defaultConfig;
        }
        catch (IOException ex)
        {
            LogCriticalCreateError(ex);
            return FanConfig.Default;
        }
        catch (UnauthorizedAccessException ex)
        {
            LogCriticalCreateError(ex);
            return FanConfig.Default;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to load config file. Reverting to defaults.")]
    private partial void LogLoadError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Configuration saved successfully.")]
    private partial void LogSaveSuccess();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to save config file.")]
    private partial void LogSaveError(Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Critical error: Could not create config directory/file.")]
    private partial void LogCriticalCreateError(Exception ex);
}

[JsonSerializable(typeof(FanConfig))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true
)]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}