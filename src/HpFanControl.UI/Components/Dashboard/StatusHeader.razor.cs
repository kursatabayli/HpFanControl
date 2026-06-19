using Microsoft.AspNetCore.Components;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using MudBlazor;

namespace HpFanControl.UI.Components.Dashboard;

#pragma warning disable CA1515
public sealed partial class StatusHeader : ComponentBase, IDisposable
{
  [Inject] public IFanControllerService FanService { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;
  private string _currentModeName = "System Loading...";

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
    _currentModeName = mode switch
    {
      FanMode.Auto => "Auto Pilot",
      FanMode.Manual => "Manual Override",
      FanMode.Max => "Max Performance",
      _ => "Unknown State",
    };
  }

  private async Task OpenSettings()
  {
    var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
    await DialogService.ShowAsync<Shared.SettingsDialog>("Settings", options).ConfigureAwait(false);
  }

  public void Dispose()
  {
    if (FanService != null)
        FanService.ModeChanged -= OnModeChanged;

    GC.SuppressFinalize(this);
  }
}
#pragma warning restore CA1515