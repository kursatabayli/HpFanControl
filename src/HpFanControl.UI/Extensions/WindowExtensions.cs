using HpFanControl.UI.Helpers;
using HpFanControl.UI.Interop;
using HpFanControl.UI.Services;
using InfiniFrame;
using InfiniFrame.BlazorWebView;
using Microsoft.Extensions.DependencyInjection;

namespace HpFanControl.UI.Extensions;

internal static class WindowExtensions
{
    public static void ConfigureAndRunWindow(this InfiniFrameBlazorAppBuilder appBuilder, bool startHidden)
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "favicon.ico");
        IServiceProvider? serviceProvider = null;

        appBuilder.WithInfiniFrameWindowBuilder(builder =>
        {
#if !DEBUG
            builder.SetDevToolsEnabled(false).SetContextMenuEnabled(false);
#endif

            builder.SetIconFile(iconPath)
            .SetTitle("HP Fan Control")
            .SetUseOsDefaultSize(false)
            .SetChromeless(true)
            .SetResizable(true)
            .SetMaximized(true)
            .SetZoomEnabled(false)
            .SetMinSize(1600, 900)
            .Center();

            builder.RegisterWindowCreatedHandler(window =>
            {
                IntPtr windowPointer = GtkWindowHelper.GetMainWindowPointer(window.Title);
                if (windowPointer != IntPtr.Zero)
                {
                    if (startHidden)
                        NativeMethods.gtk_widget_hide(windowPointer);

                    IntPtr webViewPointer = NativeMethods.gtk_bin_get_child(windowPointer);
                    if (webViewPointer != IntPtr.Zero)
                    {
                        IntPtr gesturePointer = NativeMethods.g_object_get_data(webViewPointer, "wk-view-zoom-gesture");
                        if (gesturePointer != IntPtr.Zero)
                            NativeMethods.gtk_event_controller_set_propagation_phase(gesturePointer, 0);
                    }
                }
            });


            builder.RegisterWindowClosingHandler((window, e) =>
            {
                var windowAction = serviceProvider?.GetRequiredService<WindowActionService>();
                windowAction?.TriggerHide();
                return WindowClosingResult.Cancel;
            });
        });


        InfiniFrameBlazorApp app = appBuilder.Build();

        serviceProvider = app.ServiceProvider;

        var mainWindow = serviceProvider.GetRequiredService<IInfiniFrameWindow>();
        var windowActionService = serviceProvider.GetRequiredService<WindowActionService>();

        AppStartupHelper.ConfigureGlobalExceptions(serviceProvider, mainWindow);
        AppBootstrapper.InitializeServices(serviceProvider, mainWindow);

        if (startHidden)
            windowActionService.TriggerHide();
        else
            windowActionService.TriggerShow();

        app.Run();
    }
}