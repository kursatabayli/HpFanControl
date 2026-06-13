using Microsoft.AspNetCore.Components;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using MudBlazor;
using HpFanControl.UI.Models;

namespace HpFanControl.UI.Components.Dashboard;

#pragma warning disable CA1515
public sealed partial class ModeSelector : ComponentBase, IDisposable
{
  [Inject] public IFanControllerService FanService { get; set; } = default!;

  [Parameter] public bool IsDisabled { get; set; } = false;

  private FanMode _currentMode;

  private readonly List<ModeUiDefinition> _modes =
    [
        new(FanMode.Auto, "Auto", Icons.Material.Filled.AutoMode, Color.Info),
        new(FanMode.Manual, "Manual", Icons.Material.Filled.Tune, Color.Success),
        new(FanMode.Max, "Max", Icons.Material.Filled.RocketLaunch, Color.Error)
    ];

  protected override void OnInitialized()
  {
    _currentMode = FanService.CurrentMode;

    FanService.ModeChanged += OnModeChangedFromService;
  }

  private void OnModeChangedFromService(FanMode newMode)
  {
    _currentMode = newMode;
    InvokeAsync(StateHasChanged);
  }

  private void OnUiChange(FanMode mode)
  {
    if (IsDisabled) return;

    FanService.SetMode(mode);
  }

  public void Dispose()
  {
    FanService?.ModeChanged -= OnModeChangedFromService;
    GC.SuppressFinalize(this);
  }
}
#pragma warning restore CA1515