using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using HpFanControl.Core.Models;
using MudBlazor;

#pragma warning disable CA1515, CA2227, CA1002, CA2007, CA1716
namespace HpFanControl.UI.Components.Shared;

public sealed partial class CurveEditor : ComponentBase, IAsyncDisposable
{
  [Parameter] public List<FanCurvePoint> Points { get; set; } = [];
  [Parameter] public string Title { get; set; } = "Fan Curve";
  [Parameter] public string Color { get; set; } = "#00e5ff";
  [Parameter] public int CurrentTemp { get; set; } = 0;
  [Parameter] public EventCallback<List<FanCurvePoint>> PointsChanged { get; set; }
  [Parameter] public EventCallback OnSaveRequested { get; set; }

  [Inject] private IJSRuntime JS { get; set; } = default!;
  [Inject] private IDialogService DialogService { get; set; } = default!;

  private List<FanCurvePoint> _localPoints = [];

  private ElementReference _containerRef;
  private DotNetObjectReference<CurveEditor>? _dotNetRef;

  private readonly string _guid = Guid.NewGuid().ToString("N");

  private double _width = 600;
  private const double Height = 250;
  private const double PaddingX = 20;

  private int? _draggingIndex;
  private int? _hoverIndex;

  private string LinePath => BuildPath(false);
  private string AreaPath => BuildPath(true);

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      _dotNetRef = DotNetObjectReference.Create(this);

      await JS.InvokeVoidAsync("curveEditor.addResizeListener", _containerRef, _dotNetRef);

      double initialWidth = await JS.InvokeAsync<double>("curveEditor.getElementWidth", _containerRef);
      if (initialWidth > 0)
      {
        _width = initialWidth;
        StateHasChanged();
      }
    }
  }

  protected override void OnParametersSet()
  {
    if (!_draggingIndex.HasValue)
    {
      _localPoints = [.. Points];
    }
  }

  [JSInvokable]
  public void OnResize(double width)
  {
    if (_width != width)
    {
      _width = width;
      StateHasChanged();
    }
  }

  private void HandlePointerDown(PointerEventArgs e)
  {
    int? closestIndex = null;
    double minDistance = 20.0;

    for (int i = 0; i < _localPoints.Count; i++)
    {
      double dist = Math.Abs(e.OffsetX - GetPointX(i));
      if (dist < minDistance)
      {
        minDistance = dist;
        closestIndex = i;
      }
    }

    if (closestIndex.HasValue)
    {
      _draggingIndex = closestIndex.Value;
      UpdateSpeed(_draggingIndex.Value, e.OffsetY);
    }
  }

  private async Task HandlePointerUp(PointerEventArgs e)
  {
    if (_draggingIndex.HasValue)
    {
      _draggingIndex = null;

      await PointsChanged.InvokeAsync(_localPoints);
    }
  }

  private void HandlePointerMove(PointerEventArgs e)
  {
    if (_draggingIndex.HasValue)
    {
      UpdateSpeed(_draggingIndex.Value, e.OffsetY);
    }
    else
    {
      int? newHoverIndex = null;
      double minDistance = 20.0;

      for (int i = 0; i < _localPoints.Count; i++)
      {
        double dist = Math.Abs(e.OffsetX - GetPointX(i));
        if (dist < minDistance)
        {
          minDistance = dist;
          newHoverIndex = i;
        }
      }

      if (_hoverIndex != newHoverIndex)
      {
        _hoverIndex = newHoverIndex;
      }
    }
  }
  private async Task HandlePointerLeave(PointerEventArgs e)
  {
    _hoverIndex = null;
    await HandlePointerUp(e);
  }

  private void UpdateSpeed(int index, double currentY)
  {
    int newPwm = YToPwm(currentY);

    if (_localPoints[index].Speed != newPwm)
    {
      _localPoints[index] = _localPoints[index] with { Speed = newPwm };
    }
  }

  private double GetCurrentTempX()
  {
    if (_localPoints.Count == 0) return -100;

    double minTemp = _localPoints[0].Temperature;
    double maxTemp = _localPoints[^1].Temperature;

    double clamped = Math.Clamp(CurrentTemp, minTemp, maxTemp);
    double range = maxTemp - minTemp;
    double normalized = range > 0 ? (clamped - minTemp) / range : 0;

    return PaddingX + (normalized * (_width - 2 * PaddingX));
  }

  private double GetPointX(int index)
  {
    if (_localPoints.Count == 0) return PaddingX;

    double minTemp = _localPoints[0].Temperature;
    double maxTemp = _localPoints[^1].Temperature;
    double range = maxTemp - minTemp;

    double normalized = range > 0 ? (_localPoints[index].Temperature - minTemp) / range : 0;

    return PaddingX + (normalized * (_width - 2 * PaddingX));
  }

  private static double GetPwmY(int speed) => Height - speed / 255.0 * Height;

  private static int YToPwm(double y) => Math.Clamp((int)Math.Round((Height - y) / Height * 255.0), 0, 255);

  private static int PwmToPercent(int pwm) => (int)Math.Round(pwm / 255.0 * 100);

  private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);

  private string BuildPath(bool isArea)
  {
    if (_localPoints.Count == 0) return "";

    var sb = new StringBuilder(_localPoints.Count * 25 + 50);

    for (int i = 0; i < _localPoints.Count; i++)
    {
      sb.Append(i == 0 ? "M" : "L");

      sb.Append(' ');
      sb.Append(Invariant(GetPointX(i)));
      sb.Append(' ');
      sb.Append(Invariant(GetPwmY(_localPoints[i].Speed)));
    }

    if (isArea)
    {
      sb.Append(" L ");
      sb.Append(Invariant(GetPointX(_localPoints.Count - 1)));
      sb.Append(' ');
      sb.Append(Invariant(Height));

      sb.Append(" L ");
      sb.Append(Invariant(GetPointX(0)));
      sb.Append(' ');
      sb.Append(Invariant(Height));
      sb.Append(" Z");
    }

    return sb.ToString();
  }

  private async Task OpenAdvancedSettingsDialog()
  {
    var parameters = new DialogParameters<PointEditorDialog> { { x => x.Points, _localPoints } };
    var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

    var dialog = await DialogService.ShowAsync<PointEditorDialog>($"{Title} Point Settings", parameters, options);
    var result = await dialog.Result;

    if (result is not null && !result.Canceled && result.Data is List<FanCurvePoint> updatedPoints)
    {
      _localPoints = updatedPoints;

      await PointsChanged.InvokeAsync(_localPoints);

      if (OnSaveRequested.HasDelegate)
        await OnSaveRequested.InvokeAsync();
    }
  }

  public async ValueTask DisposeAsync()
  {
    _dotNetRef?.Dispose();
    _dotNetRef = null;

    try
    {
      await JS.InvokeVoidAsync("curveEditor.removeResizeListener", _containerRef);
    }
    catch (JSDisconnectedException)
    {
    }
    catch (TaskCanceledException)
    {
    }
  }
}
#pragma warning restore CA1515, CA2227, CA1002, CA2007, CA1716