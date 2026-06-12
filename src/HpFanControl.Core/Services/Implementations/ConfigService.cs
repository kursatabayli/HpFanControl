using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.Core.Services.Implementations;

public class ConfigService : IConfigService
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config file. Reverting to defaults.");
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

            _logger.LogInformation("Configuration saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config file.");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error: Could not create config directory/file.");
            return FanConfig.Default;
        }
    }
}

[JsonSerializable(typeof(FanConfig))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true
)]
internal partial class AppJsonContext : JsonSerializerContext
{
}