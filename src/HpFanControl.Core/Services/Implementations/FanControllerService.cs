using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Helpers;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.Core.Services.Implementations;

public class FanControllerService : IFanControllerService, IDisposable
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

        _logger.LogInformation("Starting Fan Controller Service...");

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
        _logger.LogInformation("Stopping Fan Controller Service...");

        _cts?.Cancel();

        _periodicTimer?.Dispose();

        _hardware.ForceResetFanMode();

        ResetDebounceCache();
    }

    public void SetMode(FanMode mode)
    {
        if (_currentConfig.LastMode == mode) return;

        _logger.LogInformation("Changing Fan Mode to: {Mode}", mode);

        _currentConfig.LastMode = mode;
        _configService.Save(_currentConfig);

        ApplyFanMode();

        ResetDebounceCache();

        ModeChanged?.Invoke(mode);
    }

    public void LoadConfig(FanConfig config)
    {
        _logger.LogInformation("Loading new configuration...");
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
            while (timer != null && await timer.WaitForNextTickAsync(token))
            {
                UpdateCycle();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical Loop Error in Fan Controller.");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update cycle.");
        }
    }

    private void ApplyFanMode()
    {
        try
        {
            _hardware.SetFanMode(_currentConfig.LastMode);
            CurrentMode = _currentConfig.LastMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply fan mode.");
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
}