using HpFanControl.UI.Interop;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using System.Reflection;
using HpFanControl.Core.Helpers;

namespace HpFanControl.UI.Services;

#pragma warning disable CA1812
internal sealed class LinuxTrayService(
    IFanControllerService fanService,
    WindowActionService windowService) : IDisposable
{
    private readonly IFanControllerService _fanService = fanService;
    private readonly WindowActionService _windowService = windowService;
    private const string IndicatorCategory = "utilities-system-monitor";
    private const string GtkSignalActivate = "activate";

    private bool _disposed;

    private bool _isUpdatingUi;
    private IntPtr _indicator;
    private IntPtr _menu;
    private IntPtr _menuItemAuto;
    private IntPtr _menuItemManual;
    private IntPtr _menuItemMax;
    private IntPtr _menuItemToggleUi;

    private Action<Action>? _invokeOnUI;
    private Action? _onExitRequested;

    private readonly List<NativeMethods.GCallback> _keepAliveDelegates = [];
    private Action? _onVisibilityChangedDelegate;
    private readonly string _iconAuto = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "fan-auto.svg");
    private readonly string _iconManual = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "fan-manual.svg");
    private readonly string _iconMax = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "fan-max.svg");

    public void Initialize(Action<Action> invokeOnUI, Action onExitRequested)
    {
        _invokeOnUI = invokeOnUI;
        _onExitRequested = onExitRequested;

        NativeMethods.gtk_init(IntPtr.Zero, IntPtr.Zero);

        _indicator = NativeMethods.app_indicator_new(AppInfo.Name, IndicatorCategory, 1);
        _menu = NativeMethods.gtk_menu_new();

        _menuItemToggleUi = NativeMethods.gtk_menu_item_new_with_label("Hide");

        NativeMethods.GCallback toggleCallback = (widget, data) =>
        {
           _windowService.ToggleVisibility();
        };

        _keepAliveDelegates.Add(toggleCallback);
        NativeMethods.g_signal_connect_data(_menuItemToggleUi, GtkSignalActivate, toggleCallback, IntPtr.Zero, IntPtr.Zero, 0);
        NativeMethods.gtk_menu_shell_append(_menu, _menuItemToggleUi);

        _onVisibilityChangedDelegate = () =>
        {
            _invokeOnUI?.Invoke(() =>
            {
                string newLabel = _windowService.IsVisible ? "Hide" : "Show";
                NativeMethods.gtk_menu_item_set_label(_menuItemToggleUi, newLabel);
            });
        };
        _windowService.OnVisibilityChanged += _onVisibilityChangedDelegate;

        AddSeparator();

        AddHeader("Fan Modes");

        _menuItemAuto = AddRadioMenuItem("Auto", FanMode.Auto);
        _menuItemManual = AddRadioMenuItem("Manual", FanMode.Manual);
        _menuItemMax = AddRadioMenuItem("Max", FanMode.Max);

        AddSeparator();

        AddMenuItem("Exit", _onExitRequested!);

        ApplyMenuState(_fanService.CurrentMode);

        NativeMethods.gtk_widget_show_all(_menu);
        NativeMethods.app_indicator_set_menu(_indicator, _menu);
        NativeMethods.app_indicator_set_status(_indicator, 1);

        _fanService.ModeChanged += OnFanModeChanged;
    }

    private void OnFanModeChanged(FanMode newMode)
    {
        _invokeOnUI?.Invoke(() =>
        {
            ApplyMenuState(newMode);
        });
    }

    private void ApplyMenuState(FanMode newMode)
    {
        _isUpdatingUi = true;

        NativeMethods.gtk_check_menu_item_set_active(_menuItemAuto, 0);
        NativeMethods.gtk_check_menu_item_set_active(_menuItemMax, 0);
        NativeMethods.gtk_check_menu_item_set_active(_menuItemManual, 0);

        switch (newMode)
        {
            case FanMode.Auto:
                NativeMethods.gtk_check_menu_item_set_active(_menuItemAuto, 1);
                NativeMethods.app_indicator_set_icon(_indicator, _iconAuto);
                break;
            case FanMode.Manual:
                NativeMethods.gtk_check_menu_item_set_active(_menuItemManual, 1);
                NativeMethods.app_indicator_set_icon(_indicator, _iconManual);
                break;
            case FanMode.Max:
                NativeMethods.gtk_check_menu_item_set_active(_menuItemMax, 1);
                NativeMethods.app_indicator_set_icon(_indicator, _iconMax);
                break;
        }

        _isUpdatingUi = false;
    }

    private IntPtr AddRadioMenuItem(string label, FanMode targetMode)
    {
        IntPtr menuItem = NativeMethods.gtk_check_menu_item_new_with_label(label);
        NativeMethods.gtk_check_menu_item_set_draw_as_radio(menuItem, 1);

        NativeMethods.GCallback callback = (widget, data) =>
        {
            if (!_isUpdatingUi)
                _fanService.SetMode(targetMode);
        };

        _keepAliveDelegates.Add(callback);
        NativeMethods.g_signal_connect_data(menuItem, GtkSignalActivate, callback, IntPtr.Zero, IntPtr.Zero, 0);
        NativeMethods.gtk_menu_shell_append(_menu, menuItem);

        return menuItem;
    }

    private void AddMenuItem(string label, Action onClick)
    {
        IntPtr menuItem = NativeMethods.gtk_menu_item_new_with_label(label);

        NativeMethods.GCallback callback = (widget, data) =>
        {
            onClick?.Invoke();
        };

        _keepAliveDelegates.Add(callback);

        NativeMethods.g_signal_connect_data(menuItem, GtkSignalActivate, callback, IntPtr.Zero, IntPtr.Zero, 0);
        NativeMethods.gtk_menu_shell_append(_menu, menuItem);
    }
    private void AddHeader(string label)
    {
        IntPtr menuItem = NativeMethods.gtk_menu_item_new_with_label(label);
        NativeMethods.gtk_widget_set_sensitive(menuItem, 0);
        NativeMethods.gtk_menu_shell_append(_menu, menuItem);
    }

    private void AddSeparator()
    {
        IntPtr separator = NativeMethods.gtk_separator_menu_item_new();
        NativeMethods.gtk_menu_shell_append(_menu, separator);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LinuxTrayService()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_onVisibilityChangedDelegate != null)
                _windowService.OnVisibilityChanged -= _onVisibilityChangedDelegate;

            _fanService.ModeChanged -= OnFanModeChanged;
            
            _keepAliveDelegates.Clear();
        }

        _disposed = true;
    }
}
#pragma warning restore CA1812