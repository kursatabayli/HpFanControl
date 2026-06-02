using System;
using System.Threading.Tasks;
using HpFanControl.UI.Helpers;
using HpFanControl.UI.Interop;
using HpFanControl.UI.Services;
using Photino.Blazor;

namespace HpFanControl.UI.Extensions;

public static class WindowExtensions
{
    public static void ConfigureAndRunWindow(this PhotinoBlazorApp app, bool startHidden, LinuxTrayService trayService)
    {
#if !DEBUG
        app.MainWindow.SetDevToolsEnabled(false).SetContextMenuEnabled(false);
#endif

        app.MainWindow
            .SetIconFile("favicon.ico")
            .SetTitle("HP Fan Control")
            .SetLogVerbosity(0)
            .SetUseOsDefaultSize(false)
            .SetChromeless(true)
            .SetSize(1600, 900)
            .Center();

        app.MainWindow.RegisterWindowCreatedHandler((sender, e) =>
        {
            IntPtr windowPointer = GtkWindowHelper.GetMainWindowPointer();
            if (windowPointer != IntPtr.Zero)
            {
                IntPtr webViewPointer = NativeMethods.gtk_bin_get_child(windowPointer);
                if (webViewPointer != IntPtr.Zero)
                {
                    IntPtr gesturePointer = NativeMethods.g_object_get_data(webViewPointer, "wk-view-zoom-gesture");
                    if (gesturePointer != IntPtr.Zero)
                        NativeMethods.gtk_event_controller_set_propagation_phase(gesturePointer, 0);
                }
            }
        });

        app.MainWindow.RegisterWindowClosingHandler((sender, e) =>
        {
            IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
            if (gtkWindow != IntPtr.Zero)
            {
                NativeMethods.gtk_widget_hide(gtkWindow);
                trayService?.SetUiVisibilityState(false);
            }
            return true;
        });

        if (startHidden)
        {
            Task.Delay(100).ContinueWith(_ =>
            {
                app.MainWindow.Invoke(() =>
                {
                    IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                    if (gtkWindow != IntPtr.Zero)
                        NativeMethods.gtk_widget_hide(gtkWindow);

                    trayService?.SetUiVisibilityState(false);
                });
            });
        }
        else
        {
            app.MainWindow.Invoke(() => trayService?.SetUiVisibilityState(true));
        }

        app.Run();
    }
}