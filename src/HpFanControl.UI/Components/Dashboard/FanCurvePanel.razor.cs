using Microsoft.AspNetCore.Components;
using MudBlazor;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;

namespace HpFanControl.UI.Components.Dashboard;

#pragma warning disable CA1515
public sealed partial class FanCurvePanel : ComponentBase, IDisposable
{
    [Inject] public IFanControllerService FanService { get; set; } = default!;
    [Inject] public IConfigService ConfigService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    public int CurrentCpuTemp { get; private set; }
    public int CurrentGpuTemp { get; private set; }

    public bool IsReadOnly { get; private set; }
    private bool _isDirty;

    private FanConfig _draftConfig = new();
    private FanConfig _activeConfig = new();

    protected override void OnInitialized()
    {
        _activeConfig = ConfigService.Load();

        _draftConfig = DeepClone(_activeConfig);

        CheckReadOnlyState();

        FanService.StatsUpdated += OnStatsUpdated;
        FanService.ModeChanged += OnModeChanged;
    }

    private void OnStatsUpdated(SystemStats newStats)
    {
        if (CurrentCpuTemp != newStats.CpuTemp || CurrentGpuTemp != newStats.GpuTemp)
        {
            CurrentCpuTemp = newStats.CpuTemp;
            CurrentGpuTemp = newStats.GpuTemp;
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnModeChanged(FanMode newMode)
    {
        CheckReadOnlyState();
        InvokeAsync(StateHasChanged);
    }

    private void OnCurveEdited()
    {
        CheckIfDirty();
    }

    private void SaveChanges()
    {
        _draftConfig.ValidateAndSortCurves();

        _draftConfig.LastMode = FanService.CurrentMode;
        ConfigService.Save(_draftConfig);

        FanService.LoadConfig(_draftConfig);

        _activeConfig = DeepClone(_draftConfig);

        CheckIfDirty();

        Snackbar.Add("Fan configuration saved and applied.", Severity.Success);
    }

    private void DiscardChanges()
    {
        _draftConfig = DeepClone(_activeConfig);
        CheckIfDirty();
        StateHasChanged();
    }

    private void RestoreDefaults()
    {
        _draftConfig = DeepClone(FanConfig.Default);
        CheckIfDirty();
        StateHasChanged();
    }

    private void CheckReadOnlyState()
    {
        bool newState = FanService.CurrentMode != FanMode.Manual;
        if (IsReadOnly != newState)
        {
            IsReadOnly = newState;
        }
    }

    private void CheckIfDirty()
    {
        bool isChanged = !AreConfigsEqual(_activeConfig, _draftConfig);

        if (_isDirty != isChanged)
        {
            _isDirty = isChanged;
            InvokeAsync(StateHasChanged);
        }
    }

    private static FanConfig DeepClone(FanConfig source)
    {
        return new FanConfig
        {
            LastMode = source.LastMode,
            CpuCurve = [.. source.CpuCurve],
            GpuCurve = [.. source.GpuCurve]
        };
    }

    private static bool AreConfigsEqual(FanConfig a, FanConfig b)
    {
        if (a.LastMode != b.LastMode) return false;

        if (!a.CpuCurve.SequenceEqual(b.CpuCurve)) return false;
        if (!a.GpuCurve.SequenceEqual(b.GpuCurve)) return false;

        return true;
    }

    public void Dispose()
    {
        if (FanService != null)
        {
            FanService.StatsUpdated -= OnStatsUpdated;
            FanService.ModeChanged -= OnModeChanged;
        }

        GC.SuppressFinalize(this);
    }
}