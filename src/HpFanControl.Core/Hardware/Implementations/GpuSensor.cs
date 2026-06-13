using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class GpuSensor : IGpuSensor
{
    private readonly ILogger<GpuSensor> _logger;
    private readonly IGpuProvider _nvidiaProvider;
    private readonly IGpuProvider _integratedProvider;

    public GpuSensor(
            ILogger<GpuSensor> logger,
            [FromKeyedServices("Discrete")] IGpuProvider nvidiaProvider,
            [FromKeyedServices("Integrated")] IGpuProvider integratedProvider)
    {
        _logger = logger;
        _nvidiaProvider = nvidiaProvider;
        _integratedProvider = integratedProvider;

        InitializeProviders();
    }

    private void InitializeProviders()
    {
        try
        {
            _nvidiaProvider.Initialize();
        }
        catch (DllNotFoundException ex)
        {
            LogNvidiaInitError(ex);
        }
        catch (NotSupportedException ex)
        {
            LogNvidiaInitError(ex);
        }
        catch (InvalidOperationException ex)
        {
            LogNvidiaInitError(ex);
        }

        try
        {
            _integratedProvider.Initialize();
        }
        catch (UnauthorizedAccessException ex)
        {
            LogIntegratedInitError(ex);
        }
        catch (IOException ex)
        {
            LogIntegratedInitError(ex);
        }
        catch (InvalidOperationException ex)
        {
            LogIntegratedInitError(ex);
        }
    }

    public int ReadTemperature()
    {
        if (_nvidiaProvider.IsAvailable && _nvidiaProvider.IsActive)
        {
            int nvTemp = _nvidiaProvider.GetTemperature();
            if (nvTemp > 0) return nvTemp;
        }

        return _integratedProvider.GetTemperature();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to initialize Nvidia Provider")]
    private partial void LogNvidiaInitError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to initialize Integrated Provider")]
    private partial void LogIntegratedInitError(Exception ex);
}