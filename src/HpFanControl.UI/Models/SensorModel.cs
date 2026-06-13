namespace HpFanControl.UI.Models;

internal sealed class SensorModel
{
  public string Label { get; set; } = string.Empty;
  public string Icon { get; set; } = string.Empty;
  public int Temp { get; private set; }
  public int Rpm { get; private set; }
  public string DisplayColor { get; private set; } = "#ffffff";

  public bool Update(int newTemp, int newRpm, string newColor)
  {
    bool changed = false;

    if (Temp != newTemp)
    {
      Temp = newTemp;
      DisplayColor = newColor;
      changed = true;
    }

    if (Rpm != newRpm)
    {
      Rpm = newRpm;
      changed = true;
    }

    return changed;
  }
}