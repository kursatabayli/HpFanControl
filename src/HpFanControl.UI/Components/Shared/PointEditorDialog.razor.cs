using HpFanControl.Core.Helpers;
using HpFanControl.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HpFanControl.UI.Components.Shared;

public partial class PointEditorDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Parameter] public List<FanCurvePoint> Points { get; set; } = [];

    private List<FanCurvePoint> _editPoints = [];
    private int? _newTemp;

    protected override void OnInitialized() => _editPoints = [.. Points];

    private void AddNewPoint()
    {
        if (!_newTemp.HasValue) return;
        int temp = _newTemp.Value;

        if (temp <= 0 || temp >= 100)
        {
            Snackbar.Add("Temperature must be between 1 and 99 °C.", Severity.Warning);
            return;
        }

        if (_editPoints.Any(p => p.Temperature == temp))
        {
            Snackbar.Add($"A point at {temp}°C already exists.", Severity.Warning);
            return;
        }

        int autoSpeed = FanCurveCalculator.CalculatePwm(temp, _editPoints.OrderBy(p => p.Temperature).ToArray());

        _editPoints.Add(new FanCurvePoint(temp, autoSpeed));
        _editPoints = [.. _editPoints.OrderBy(p => p.Temperature)];
        
        _newTemp = null; 
    }

    private void RemovePoint(FanCurvePoint point)
    {
        if (point.Temperature == 0 || point.Temperature == 100) return;
        _editPoints.Remove(point);
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(_editPoints));

    private void Cancel() => MudDialog.Cancel();
}