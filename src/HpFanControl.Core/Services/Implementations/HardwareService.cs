using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.Core.Services.Implementations;

public sealed partial class HardwareService(
    ILogger<HardwareService> logger,
    ICpuSensor cpuSensor,
    IGpuSensor gpuSensor,
    IFanDriver fanDriver) : IHardwareService
{
    private readonly ILogger<HardwareService> _logger = logger;
    private readonly ICpuSensor _cpuSensor = cpuSensor;
    private readonly IGpuSensor _gpuSensor = gpuSensor;
    private readonly IFanDriver _fanDriver = fanDriver;

    public SystemStats GetSystemStats()
    {
        int cpuTemp = _cpuSensor.ReadTemperature();
        int gpuTemp = _gpuSensor.ReadTemperature();

        var (cpuRpm, gpuRpm) = _fanDriver.GetRpms();

        return new SystemStats(cpuTemp, gpuTemp, cpuRpm, gpuRpm);
    }

    public void SetFanMode(FanMode mode)
    {
        _fanDriver.SetMode(mode);
        if (_logger.IsEnabled(LogLevel.Information))
            LogFanModeSet(mode);
    }

    public void SetFanSpeed(bool isGpu, int pwmValue) => _fanDriver.SetSpeed(isGpu, pwmValue);

    public void ForceResetFanMode()
    {
        try
        {
            _fanDriver.SetMode(FanMode.Auto);
            LogFanModeForcedAuto();
        }
        catch (IOException ex)
        {
            LogForceResetError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogForceResetError(ex);
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Fan mode set to: {Mode}")]
    private partial void LogFanModeSet(FanMode mode);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Fan mode forced to Auto (Reset).")]
    private partial void LogFanModeForcedAuto();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to force reset fan mode.")]
    private partial void LogForceResetError(Exception ex);
    #endregion
}