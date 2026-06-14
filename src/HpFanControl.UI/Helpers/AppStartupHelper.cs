using InfiniFrame;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HpFanControl.UI.Helpers;

internal static partial class AppStartupHelper
{
    public static bool ShouldStartNewInstance(string[] args)
    {
        string message = "SHOW_UI";
        bool isToggleModeCommand = false;

        if (args.Contains("--toggle-mode"))
        {
            message = "TOGGLE_MODE";
            isToggleModeCommand = true;
        }
        else if (args.Contains("--toggle-ui"))
        {
            message = "TOGGLE_UI";
        }
        else if (args.Contains("--hidden"))
        {
            message = "PING";
        }

        bool isAlreadyRunning = IpcManager.TrySendMessage(message);

        if (isAlreadyRunning)
            return false;

        if (isToggleModeCommand)
            return false;

        return true;
    }

    public static void ConfigureGlobalExceptions(IServiceProvider serviceProvider, IInfiniFrameWindow mainWindow)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            var ex = error.ExceptionObject as Exception;
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            LogFatalCrash(logger, ex);
            mainWindow.Invoke(() =>
            {
                mainWindow.ShowMessage("Fatal Error", ex?.Message ?? "Unknown error");
            });
        };
    }

    #region Logging
    [LoggerMessage(EventId = 2, Level = LogLevel.Critical, Message = "Fatal application crash detected!")]
    private static partial void LogFatalCrash(ILogger logger, Exception? ex);
    #endregion
}