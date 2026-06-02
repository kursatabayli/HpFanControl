using System;
using System.Linq;
using HpFanControl.UI.Extensions;
using HpFanControl.UI.Helpers;
using Photino.Blazor;

namespace HpFanControl.UI;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (AppStartupHelper.HandleCommandLineArgs(args)) return;

        using var appMutex = AppStartupHelper.EnsureSingleInstance();

        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);
        appBuilder.Services.AddApplicationServices();
        appBuilder.RootComponents.Add<App>("app");
        
        var app = appBuilder.Build();

        AppStartupHelper.ConfigureGlobalExceptions(app);
        var trayService = AppBootstrapper.InitializeServices(app);

        bool startHidden = args.Contains("--hidden");
        app.ConfigureAndRunWindow(startHidden, trayService);
    }
}