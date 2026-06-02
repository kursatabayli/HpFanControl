using System;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using HpFanControl.UI.Interop;
using HpFanControl.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;

namespace HpFanControl.UI.Helpers;

public static class AppBootstrapper
{
    public static LinuxTrayService InitializeServices(PhotinoBlazorApp app)
    {
        var serviceProvider = app.Services;
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        LinuxTrayService trayService = null;

        try
        {
            logger.LogInformation("Application bootstrapping...");

            var configService = serviceProvider.GetRequiredService<IConfigService>();
            var config = configService.Load();

            var fanController = serviceProvider.GetRequiredService<IFanControllerService>();
            fanController.LoadConfig(config);
            fanController.Start();

            trayService = new LinuxTrayService(
                fanController,
                invokeOnUI: action => app.MainWindow.Invoke(action),
                onShowUiRequested: () =>
                {
                    IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                    if (gtkWindow != IntPtr.Zero)
                    {
                        NativeMethods.gtk_window_present(gtkWindow);
                        trayService.SetUiVisibilityState(true);
                    }
                },
                onHideUiRequested: () =>
                {
                    IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                    if (gtkWindow != IntPtr.Zero)
                    {
                        NativeMethods.gtk_widget_hide(gtkWindow);
                        trayService.SetUiVisibilityState(false);
                    }
                },
                onExitRequested: () =>
                {
                    fanController.Stop();
                    Environment.Exit(0);
                }
            );

            var windowAction = serviceProvider.GetRequiredService<WindowActionService>();
            windowAction.OnHideRequested = () =>
            {
                app.MainWindow.Invoke(() =>
                {
                    IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                    if (gtkWindow != IntPtr.Zero)
                    {
                        NativeMethods.gtk_widget_hide(gtkWindow);
                        trayService?.SetUiVisibilityState(false);
                    }
                });
            };

            windowAction.OnMinimizeRequested = () => app.MainWindow.Invoke(() => app.MainWindow.SetMinimized(true));
            
            bool isMaximized = false;
            windowAction.OnMaximizeRequested = () =>
            {
                app.MainWindow.Invoke(() =>
                {
                    isMaximized = !isMaximized;
                    app.MainWindow.SetMaximized(isMaximized);
                });
            };

            windowAction.OnResizeRequested = (edge) =>
            {
                app.MainWindow.Invoke(() =>
                {
                    IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                    if (gtkWindow != IntPtr.Zero)
                    {
                        NativeMethods.gtk_window_begin_resize_drag(gtkWindow, edge, 1, 0, 0, 0);
                    }
                });
            };

            IpcManager.StartServer(message =>
            {
                if (message == "TOGGLE_UI")
                {
                    app.MainWindow.Invoke(() =>
                    {
                        IntPtr gtkWindow = GtkWindowHelper.GetMainWindowPointer();
                        if (gtkWindow != IntPtr.Zero)
                        {
                            if (trayService != null && trayService.IsUiVisible)
                            {
                                NativeMethods.gtk_widget_hide(gtkWindow);
                                trayService.SetUiVisibilityState(false);
                            }
                            else
                            {
                                NativeMethods.gtk_window_present(gtkWindow);
                                trayService?.SetUiVisibilityState(true);
                            }
                        }
                    });
                }
                else if (message == "TOGGLE_MODE")
                {
                    app.MainWindow.Invoke(() =>
                    {
                        var next = fanController.CurrentMode switch
                        {
                            FanMode.Auto => FanMode.Manual,
                            FanMode.Manual => FanMode.Max,
                            FanMode.Max => FanMode.Auto,
                            _ => FanMode.Auto
                        };
                        fanController.SetMode(next);
                    });
                }
            });

            return trayService;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize services.");
            return null;
        }
    }
}