using HpFanControl.Core.Models;

namespace HpFanControl.Core.Services.Interfaces;

#pragma warning disable CA1003, CA1716
public interface IFanControllerService
{
    FanMode CurrentMode { get; }
    event Action<SystemStats>? StatsUpdated;

    event Action<FanMode>? ModeChanged;

    void Start();

    void Stop();

    void LoadConfig(FanConfig config);

    void SetMode(FanMode mode);
}
#pragma warning restore CA1003, CA1716