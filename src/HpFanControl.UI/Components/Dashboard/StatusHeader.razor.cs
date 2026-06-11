using Microsoft.AspNetCore.Components;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using MudBlazor;

namespace HpFanControl.UI.Components.Dashboard;

public partial class StatusHeader : ComponentBase, IDisposable
{
  [Inject] public IFanControllerService FanService { get; set; } = default!;
  private string _currentModeName = "System Loading...";
  private string _currentIcon = Icons.Material.Filled.QuestionMark;

  protected override void OnInitialized()
  {
    UpdateState(FanService.CurrentMode);
    FanService.ModeChanged += OnModeChanged;
  }

  private void OnModeChanged(FanMode mode)
  {
    UpdateState(mode);
    InvokeAsync(StateHasChanged);
  }

  private void UpdateState(FanMode mode)
  {
    switch (mode)
    {
      case FanMode.Auto:
        _currentModeName = "Auto Pilot";
        _currentIcon = Icons.Material.Filled.AutoMode;
        break;

      case FanMode.Manual:
        _currentModeName = "Manual Override";
        _currentIcon = Icons.Material.Filled.Tune;
        break;

      case FanMode.Max:
        _currentModeName = "Max Performance";
        _currentIcon = Icons.Material.Filled.RocketLaunch;
        break;

      default:
        _currentModeName = "Unknown State";
        _currentIcon = Icons.Material.Filled.QuestionMark;
        break;
    }
  }

  public void Dispose()
  {
    FanService?.ModeChanged -= OnModeChanged;
  }
}