using HpFanControl.UI.Helpers;
using HpFanControl.UI.Interop;
using InfiniFrame;

namespace HpFanControl.UI.Services;

public class WindowActionService
{
    private IInfiniFrameWindow _window;

    public bool IsVisible { get; private set; } = true;
    public bool IsMaximized { get; private set; } = true;
    public event Action OnVisibilityChanged;

    public void Initialize(IInfiniFrameWindow window)
    {
        _window = window;
    }

    public void TriggerHide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        _window?.Invoke(() =>
        {
            IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer(_window.Title);
            if (gtkWindow != IntPtr.Zero)
                NativeMethods.gtk_widget_hide(gtkWindow);
        });

        OnVisibilityChanged?.Invoke();
    }

    public void TriggerShow()
    {
        if (IsVisible) return;
        IsVisible = true;

        _window?.Invoke(() =>
        {
            IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer(_window.Title);
            if (gtkWindow != IntPtr.Zero)
                NativeMethods.gtk_window_present(gtkWindow);
        });

        OnVisibilityChanged?.Invoke();
    }

    public void TriggerMinimize()
    {
        _window?.Invoke(() => _window.SetMinimized(true));
    }

    public void TriggerMaximize()
    {
        IsMaximized = !IsMaximized;
        _window?.Invoke(() => _window.SetMaximized(IsMaximized));
    }

    public void TriggerResize(int edge)
    {
        _window?.Invoke(() =>
        {
            IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer(_window.Title);
            if (gtkWindow != IntPtr.Zero)
                NativeMethods.gtk_window_begin_resize_drag(gtkWindow, edge, 1, 0, 0, 0);
        });
    }

    public void TriggerDragMove()
    {
        _window?.Invoke(() =>
        {
            IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer(_window.Title);
            if (gtkWindow != IntPtr.Zero)
                NativeMethods.gtk_window_begin_move_drag(gtkWindow, 1, 0, 0, 0);
        });
    }

    public void ToggleVisibility()
    {
        if (IsVisible) TriggerHide();
        else TriggerShow();
    }
}