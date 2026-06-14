using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Helpers;
using System.Text;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class CpuSensor(ILogger<CpuSensor> logger) : ICpuSensor
{
    private readonly ILogger<CpuSensor> _logger = logger;
    private static readonly byte[][] PriorityDrivers =
    [
        "k10temp"u8.ToArray(),
        "coretemp"u8.ToArray(),
        "acpitz"u8.ToArray()
    ];

    private string? _detectedPath;
    private FileStream? _stream;

    private readonly byte[] _tempBuffer = new byte[16];

    public int ReadTemperature()
    {
        if (_detectedPath == null)
        {
            FindPath();
            if (_detectedPath == null) return 0;
        }

        return SysFs.ReadInt(ref _stream, _detectedPath!, _tempBuffer) / 1000;
    }

    public void FindPath()
    {
        if (!Directory.Exists(LinuxSysFsContracts.HwmonBaseDir))
        {
            LogHwmonNotFound();
            return;
        }

        try
        {
            var directories = Directory.GetDirectories(LinuxSysFsContracts.HwmonBaseDir);

            foreach (var targetDriver in PriorityDrivers)
            {
                foreach (var dir in directories)
                {
                    var namePath = Path.Combine(dir, LinuxSysFsContracts.FileName);

                    FileStream? fs = null;
                    bool match = false;
                    try
                    {
                        match = SysFs.CheckContentEquals(ref fs, namePath, targetDriver, _tempBuffer);
                    }
                    finally
                    {
                        fs?.Dispose();
                    }

                    if (match)
                    {
                        var potentialPath = Path.Combine(dir, LinuxSysFsContracts.FileTemp1Input);

                        if (File.Exists(potentialPath))
                        {
                            _detectedPath = potentialPath;
                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                var driverStr = Encoding.UTF8.GetString(targetDriver);
                                LogSensorDetected(driverStr, _detectedPath);
                            }

                            _stream?.Dispose();
                            _stream = null;

                            return;
                        }
                    }
                }
            }

            LogNoSensorFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            LogSensorError(ex);
        }
        catch (IOException ex)
        {
            LogSensorError(ex);
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;

        GC.SuppressFinalize(this);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Hwmon directory not found.")]
    private partial void LogHwmonNotFound();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "CPU Sensor detected: {Driver} at {Path}")]
    private partial void LogSensorDetected(string driver, string path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "No compatible CPU thermal sensor found.")]
    private partial void LogNoSensorFound();

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error while scanning for CPU sensor.")]
    private partial void LogSensorError(Exception ex);
    #endregion
}