using System;
using System.Collections.Generic;
using HpFanControl.UI.Interop;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using System.Threading.Tasks;
using System.IO;

namespace HpFanControl.UI.Services;

public class LinuxTrayService
{
    public bool IsUiVisible { get; private set; } = false;

    private bool _isUpdatingUi = false;
    private IntPtr _indicator;
    private IntPtr _menu;
    private IntPtr _menuItemAuto;
    private IntPtr _menuItemManual;
    private IntPtr _menuItemMax;
    private IntPtr _menuItemToggleUi;

    private readonly List<NativeMethods.GCallback> _keepAliveDelegates = [];
    private readonly IFanControllerService _fanService;
    private readonly Action<Action> _invokeOnUI;
    private readonly Action _onShowUiRequested;
    private readonly Action _onHideUiRequested;
    private readonly Action _onExitRequested;

    private readonly string _iconAuto = Path.Combine(AppContext.BaseDirectory, "wwwroot", "fan-auto.svg");
    private readonly string _iconManual = Path.Combine(AppContext.BaseDirectory, "wwwroot", "fan-manual.svg");
    private readonly string _iconMax = Path.Combine(AppContext.BaseDirectory, "wwwroot", "fan-max.svg");

    public LinuxTrayService(IFanControllerService fanService, Action<Action> invokeOnUI, Action onShowUiRequested, Action onHideUiRequested, Action onExitRequested)
    {
        _fanService = fanService;
        _invokeOnUI = invokeOnUI;
        _onShowUiRequested = onShowUiRequested;
        _onHideUiRequested = onHideUiRequested;
        _onExitRequested = onExitRequested;

        InitializeTray();
    }

    private void InitializeTray()
    {
        NativeMethods.gtk_init(IntPtr.Zero, IntPtr.Zero);

        _indicator = NativeMethods.app_indicator_new("hp-fan-control", "utilities-system-monitor", 1);
        _menu = NativeMethods.gtk_menu_new();

        _menuItemToggleUi = NativeMethods.gtk_menu_item_new_with_label("Show");
        NativeMethods.GCallback toggleCallback = (widget, data) =>
        {
            if (IsUiVisible)
                _onHideUiRequested?.Invoke();
            else
                _onShowUiRequested?.Invoke();
        };
        _keepAliveDelegates.Add(toggleCallback);
        NativeMethods.g_signal_connect_data(_menuItemToggleUi, "activate", toggleCallback, IntPtr.Zero, IntPtr.Zero, 0);
        NativeMethods.gtk_menu_shell_append(_menu, _menuItemToggleUi);
        AddSeparator();

        AddHeader("Fan Modes");

        _menuItemAuto = AddRadioMenuItem("Auto", FanMode.Auto);
        _menuItemManual = AddRadioMenuItem("Manual", FanMode.Manual);
        _menuItemMax = AddRadioMenuItem("Max", FanMode.Max);

        AddSeparator();
        AddMenuItem("Exit", _onExitRequested);

        NativeMethods.app_indicator_set_menu(_indicator, _menu);
        NativeMethods.gtk_widget_show_all(_menu);
        NativeMethods.app_indicator_set_status(_indicator, 1);

        _fanService.ModeChanged += OnFanModeChanged;

        Task.Run(async () =>
        {
            await Task.Delay(100);

            _invokeOnUI(() =>
            {
                ApplyMenuState(_fanService.CurrentMode);
            });
        });
    }

    public void SetUiVisibilityState(bool isVisible)
    {
        _invokeOnUI(() =>
        {
            IsUiVisible = isVisible;
            string newLabel = isVisible ? "Hide" : "Show";
            NativeMethods.gtk_menu_item_set_label(_menuItemToggleUi, newLabel);
        });
    }
    private void OnFanModeChanged(FanMode newMode)
    {
        _invokeOnUI(() =>
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
            {
                _fanService.SetMode(targetMode);
            }
        };

        _keepAliveDelegates.Add(callback);
        NativeMethods.g_signal_connect_data(menuItem, "activate", callback, IntPtr.Zero, IntPtr.Zero, 0);
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

        NativeMethods.g_signal_connect_data(menuItem, "activate", callback, IntPtr.Zero, IntPtr.Zero, 0);
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
}