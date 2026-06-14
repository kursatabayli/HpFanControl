using HpFanControl.Core.Models;
using HpFanControl.Core.Services.Interfaces;
using HpFanControl.UI.Services;
using InfiniFrame;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HpFanControl.UI.Helpers;

internal static partial class AppBootstrapper
{
    public static void InitializeServices(IServiceProvider serviceProvider, IInfiniFrameWindow mainWindow)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            LogBootstrapping(logger);

            var configService = serviceProvider.GetRequiredService<IConfigService>();
            var config = configService.Load();

            var fanController = serviceProvider.GetRequiredService<IFanControllerService>();
            fanController.LoadConfig(config);
            fanController.Start();

            var windowAction = serviceProvider.GetRequiredService<WindowActionService>();

            var trayService = serviceProvider.GetRequiredService<LinuxTrayService>();

            trayService.Initialize(
                invokeOnUI: mainWindow.Invoke,
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
            LogBootstrapCritical(logger, ex);
            throw;
        }
    }

    #region Logging
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Application bootstrapping...")]
    private static partial void LogBootstrapping(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Failed to initialize services.")]
    private static partial void LogBootstrapCritical(ILogger logger, Exception ex);
    #endregion
}