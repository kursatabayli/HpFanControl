using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;

namespace HpFanControl.UI.Helpers;

public static class AppStartupHelper
{
    public static bool HandleCommandLineArgs(string[] args)
    {
        if (args.Contains("--toggle-ui"))
        {
            IpcManager.SendMessage("TOGGLE_UI");
            return true;
        }
        if (args.Contains("--toggle-mode"))
        {
            IpcManager.SendMessage("TOGGLE_MODE");
            return true;
        }
        return false;
    }

    public static Mutex EnsureSingleInstance()
    {
        var mutex = new Mutex(true, "HpFanControl_Mutex_" + Environment.UserName, out bool createdNew);
        if (!createdNew)
        {
            IpcManager.SendMessage("TOGGLE_UI");
            Environment.Exit(0);
        }
        return mutex;
    }

    public static void ConfigureGlobalExceptions(PhotinoBlazorApp app)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            var ex = error.ExceptionObject as Exception;
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Fatal application crash detected!");
            app.MainWindow.ShowMessage("Fatal Error", ex?.Message ?? "Unknown error");
        };
    }
}