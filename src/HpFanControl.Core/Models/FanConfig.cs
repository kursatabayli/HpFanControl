namespace HpFanControl.Core.Models;

public class FanConfig
{
    public List<FanCurvePoint> CpuCurve { get; set; } = [];
    public List<FanCurvePoint> GpuCurve { get; set; } = [];
    public FanMode LastMode { get; set; } = FanMode.Auto;

    public static FanConfig Default => new()
    {
        LastMode = FanMode.Auto,
        CpuCurve = CreateDefaultCurve(),
        GpuCurve = CreateDefaultCurve()
    };

    private static List<FanCurvePoint> CreateDefaultCurve()
    {
        return
        [
            new (0, 0),
            new (25, 63),
            new (50, 127),
            new (75, 191),
            new (100, 255)

        ];
    }

    public void ValidateAndSortCurves()
    {
        CpuCurve = SanitizeCurve(CpuCurve);
        GpuCurve = SanitizeCurve(GpuCurve);
    }

    private static List<FanCurvePoint> SanitizeCurve(List<FanCurvePoint> curve)
    {
        curve ??= [];

        var clamped = curve.Select(p => new FanCurvePoint(
            Math.Clamp(p.Temperature, 0, 100),
            Math.Clamp(p.Speed, 0, 255)
        ));

        var distinctPoints = clamped
            .GroupBy(p => p.Temperature)
            .ToDictionary(g => g.Key, g => g.Last().Speed);

        if (!distinctPoints.ContainsKey(0))
            distinctPoints[0] = 63;
        if (!distinctPoints.ContainsKey(100))
            distinctPoints[100] = 255;

        return [.. distinctPoints
            .Select(kvp => new FanCurvePoint(kvp.Key, kvp.Value))
            .OrderBy(p => p.Temperature)];
    }
}