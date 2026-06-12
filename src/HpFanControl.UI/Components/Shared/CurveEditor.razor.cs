using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using HpFanControl.Core.Models;
using MudBlazor;

namespace HpFanControl.UI.Components.Shared;

public partial class CurveEditor : ComponentBase, IAsyncDisposable
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
  private ElementReference _svgRef;
  private DotNetObjectReference<CurveEditor>? _dotNetRef;

  private readonly string _guid = Guid.NewGuid().ToString("N");

  private double _width = 600;
  private const double Height = 250;
  private const double PaddingX = 20;

  private int? _draggingIndex = null;
  private int? _hoverIndex = null;

  private BoundingClientRect? _cachedSvgRect;

  private double StepX => _localPoints.Count > 1 ? (_width - 2 * PaddingX) / (_localPoints.Count - 1) : 0;

  private string _linePath => BuildPath(false);
  private string _areaPath => BuildPath(true);

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      _dotNetRef = DotNetObjectReference.Create(this);

      await JS.InvokeVoidAsync("curveEditor.addResizeListener", _containerRef, _dotNetRef);

      var rect = await JS.InvokeAsync<BoundingClientRect>("curveEditor.getBoundingClientRect", _containerRef);
      if (rect != null && rect.Width > 0)
      {
        _width = rect.Width;
        StateHasChanged();
      }
    }
  }

  protected override void OnParametersSet()
  {
    if (!_draggingIndex.HasValue)
    {
      _localPoints = Points.ToList();
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
    int count = _localPoints.Count;
    double halfStep = StepX / 2;

    for (int i = 0; i < count; i++)
    {
      if (Math.Abs(e.OffsetX - GetPointX(i)) < halfStep)
      {
        _draggingIndex = i;
        UpdateSpeed(i, e.OffsetY);
        break;
      }
    }
  }

  private async Task HandlePointerUp(PointerEventArgs e)
  {
    if (_draggingIndex.HasValue)
    {
      _draggingIndex = null;
      _cachedSvgRect = null;

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
      int count = _localPoints.Count;
      double halfStep = StepX / 2;
      int? newHoverIndex = null;

      for (int i = 0; i < count; i++)
      {
        if (Math.Abs(e.OffsetX - GetPointX(i)) < halfStep)
        {
          newHoverIndex = i;
          break;
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

  private double GetPointX(int index) => PaddingX + index * StepX;

  private static double GetPwmY(int speed) => Height - (speed / 255.0) * Height;

  private static int YToPwm(double y) => Math.Clamp((int)Math.Round(((Height - y) / Height) * 255.0), 0, 255);

  private static int PwmToPercent(int pwm) => (int)Math.Round((pwm / 255.0) * 100);

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

        if (!result.Canceled && result.Data is List<FanCurvePoint> updatedPoints)
        {
            _localPoints = updatedPoints;
            
            await PointsChanged.InvokeAsync(_localPoints); 
            
            if (OnSaveRequested.HasDelegate)
                await OnSaveRequested.InvokeAsync();
        }
    }

  public async ValueTask DisposeAsync()
  {
    if (_dotNetRef != null)
    {
      _dotNetRef.Dispose();
      _dotNetRef = null;
    }

    try
    {
      await JS.InvokeVoidAsync("curveEditor.removeResizeListener", _containerRef);
    }
    catch
    {
    }
  }

  public class BoundingClientRect
  {
    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
  }
}