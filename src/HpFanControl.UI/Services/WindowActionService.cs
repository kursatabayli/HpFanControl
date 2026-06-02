using System;

namespace HpFanControl.UI.Services;

public class WindowActionService
{
    public Action OnHideRequested { get; set; }
    public Action OnMinimizeRequested { get; set; }
    public Action OnMaximizeRequested { get; set; }

    public Action<int> OnResizeRequested { get; set; }
}