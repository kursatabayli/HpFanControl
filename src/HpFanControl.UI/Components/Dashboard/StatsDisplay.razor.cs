using Microsoft.AspNetCore.Components;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using MudBlazor;
using HpFanControl.UI.Models;
using System;

namespace HpFanControl.UI.Components.Dashboard;

public partial class StatsDisplay : ComponentBase, IDisposable
{
  [Inject] public IFanControllerService FanService { get; set; } = default!;
  private readonly SensorModel[] _sensors = new SensorModel[2];
  private static readonly string[] _colorCache = new string[101];

  static StatsDisplay()
  {
    for (int i = 0; i <= 100; i++)
    {
      _colorCache[i] = GenerateHslColor(i);
    }
  }

  protected override void OnInitialized()
  {
    _sensors[0] = new SensorModel { Label = "CPU", Icon = Icons.Material.Filled.Memory };
    _sensors[1] = new SensorModel { Label = "GPU", Icon = Icons.Material.Filled.DeveloperBoard };

    UpdateSensors(new SystemStats(0, 0, 0, 0));

    FanService.StatsUpdated += OnStatsUpdated;
  }

  private void OnStatsUpdated(SystemStats stats)
  {
    if (UpdateSensors(stats))
    {
      InvokeAsync(StateHasChanged);
    }
  }

  private bool UpdateSensors(SystemStats stats)
  {
    bool cpuChanged = _sensors[0].Update(stats.CpuTemp, stats.CpuFanRpm, GetCachedColor(stats.CpuTemp));
    bool gpuChanged = _sensors[1].Update(stats.GpuTemp, stats.GpuFanRpm, GetCachedColor(stats.GpuTemp));

    return cpuChanged || gpuChanged;
  }

  private static string GetCachedColor(int temp)
  {
    if (temp < 0) return _colorCache[0];
    if (temp > 100) return _colorCache[100];
    return _colorCache[temp];
  }

  private static string GenerateHslColor(int temp)
  {
    double minTemp = 40.0;
    double maxTemp = 90.0;

    double clamped = Math.Clamp(temp, minTemp, maxTemp);
    double ratio = (clamped - minTemp) / (maxTemp - minTemp);
    double hue = 120 - (ratio * 120);

    return $"hsl({(int)hue}, 85%, 60%)";
  }

  public void Dispose()
  {
    if (FanService != null)
    {
      FanService.StatsUpdated -= OnStatsUpdated;
    }
  }
}