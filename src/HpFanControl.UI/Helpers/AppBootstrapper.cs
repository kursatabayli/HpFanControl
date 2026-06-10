using System;
using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using HpFanControl.UI.Services;
using InfiniFrame;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HpFanControl.UI.Helpers;

public static class AppBootstrapper
{
    private static LinuxTrayService? _trayService;
    public static void InitializeServices(IServiceProvider serviceProvider, IInfiniFrameWindow mainWindow)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Application bootstrapping...");

            var configService = serviceProvider.GetRequiredService<IConfigService>();
            var config = configService.Load();

            var fanController = serviceProvider.GetRequiredService<IFanControllerService>();
            fanController.LoadConfig(config);
            fanController.Start();

            var windowAction = serviceProvider.GetRequiredService<WindowActionService>();
            windowAction.Initialize(mainWindow);

            _trayService = new LinuxTrayService(
                fanController,
                windowAction,
                invokeOnUI: action => mainWindow.Invoke(action),
                onExitRequested: () =>
                {
                    fanController.Stop();
                    Environment.Exit(0);
                }
            );

            IpcManager.StartServer(message =>
            {
                if (message == "PING") return;
                
                if (message == "TOGGLE_UI")
                {
                    windowAction.ToggleVisibility();
                }
                else if (message == "TOGGLE_MODE")
                {
                    mainWindow.Invoke(() =>
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
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize services.");
        }
    }
}