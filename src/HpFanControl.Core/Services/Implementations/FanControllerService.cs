using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Helpers;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.Core.Services.Implementations;

public sealed partial class FanControllerService : IFanControllerService, IDisposable
{
    private readonly ILogger<FanControllerService> _logger;
    private readonly IHardwareService _hardware;
    private readonly IConfigService _configService;

    private FanConfig _currentConfig;

    private PeriodicTimer? _periodicTimer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private int _lastAppliedCpuPwm = -1;
    private int _lastAppliedGpuPwm = -1;

    public FanMode CurrentMode { get; private set; }

    public event Action<SystemStats>? StatsUpdated;
    public event Action<FanMode>? ModeChanged;

    public FanControllerService(
        ILogger<FanControllerService> logger,
        IHardwareService hardware,
        IConfigService configService)
    {
        _logger = logger;
        _hardware = hardware;
        _configService = configService;

        _currentConfig = FanConfig.Default;
        CurrentMode = _currentConfig.LastMode;
    }

    public void Start()
    {
        if (_loopTask != null && !_loopTask.IsCompleted) return;

        LogStarting();

        _currentConfig = _configService.Load();

        CurrentMode = _currentConfig.LastMode;
        ModeChanged?.Invoke(CurrentMode);

        ApplyFanMode();

        _cts = new CancellationTokenSource();
        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        _loopTask = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        LogStopping();

        _cts?.Cancel();

        _periodicTimer?.Dispose();

        _hardware.ForceResetFanMode();

        ResetDebounceCache();
    }

    public void SetMode(FanMode mode)
    {
        if (_currentConfig.LastMode == mode) return;

        if (_logger.IsEnabled(LogLevel.Information))
            LogChangingMode(mode);

        _currentConfig.LastMode = mode;
        _configService.Save(_currentConfig);

        ApplyFanMode();

        ResetDebounceCache();

        ModeChanged?.Invoke(mode);
    }

    public void LoadConfig(FanConfig config)
    {
        LogLoadingConfig();
        _currentConfig = config;

        if (_loopTask != null && !_loopTask.IsCompleted && CurrentMode == FanMode.Manual)
        {
            ResetDebounceCache();
            UpdateCycle();
        }

        if (CurrentMode != _currentConfig.LastMode)
        {
            CurrentMode = _currentConfig.LastMode;
            ApplyFanMode();
            ModeChanged?.Invoke(CurrentMode);
        }
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        var timer = _periodicTimer;

        try
        {
            while (timer != null && await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                UpdateCycle();
        }
        catch (OperationCanceledException)
        {
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogCriticalLoopError(ex);
        }
    }

    private void UpdateCycle()
    {
        try
        {
            var stats = _hardware.GetSystemStats();
            StatsUpdated?.Invoke(stats);

            if (CurrentMode != FanMode.Manual) return;

            var cpuSpan = CollectionsMarshal.AsSpan(_currentConfig.CpuCurve);
            var gpuSpan = CollectionsMarshal.AsSpan(_currentConfig.GpuCurve);

            int targetCpuPwm = FanCurveCalculator.CalculatePwm(stats.CpuTemp, cpuSpan);
            int targetGpuPwm = FanCurveCalculator.CalculatePwm(stats.GpuTemp, gpuSpan);

            if (targetCpuPwm != _lastAppliedCpuPwm)
            {
                _hardware.SetFanSpeed(isGpu: false, targetCpuPwm);
                _lastAppliedCpuPwm = targetCpuPwm;
            }

            if (targetGpuPwm != _lastAppliedGpuPwm)
            {
                _hardware.SetFanSpeed(isGpu: true, targetGpuPwm);
                _lastAppliedGpuPwm = targetGpuPwm;
            }
        }
        catch (IOException ex)
        {
            LogUpdateCycleError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogUpdateCycleError(ex);
        }
    }

    private void ApplyFanMode()
    {
        try
        {
            _hardware.SetFanMode(_currentConfig.LastMode);
            CurrentMode = _currentConfig.LastMode;
        }
        catch (IOException ex)
        {
            LogApplyFanModeError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogApplyFanModeError(ex);
        }
    }

    private void ResetDebounceCache()
    {
        _lastAppliedCpuPwm = -1;
        _lastAppliedGpuPwm = -1;
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _periodicTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Starting Fan Controller Service...")]
    private partial void LogStarting();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Stopping Fan Controller Service...")]
    private partial void LogStopping();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Changing Fan Mode to: {Mode}")]
    private partial void LogChangingMode(FanMode mode);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Loading new configuration...")]
    private partial void LogLoadingConfig();

    [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "Critical Loop Error in Fan Controller.")]
    private partial void LogCriticalLoopError(Exception ex);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error during update cycle.")]
    private partial void LogUpdateCycleError(Exception ex);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Failed to apply fan mode.")]
    private partial void LogApplyFanModeError(Exception ex);

    #endregion
}