using System;
using System.Linq;
using HpFanControl.UI.Extensions;
using HpFanControl.UI.Helpers;
using HpFanControl.UI.Services;
using InfiniFrame.BlazorWebView;
using Microsoft.Extensions.DependencyInjection;

namespace HpFanControl.UI;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (!AppStartupHelper.ShouldStartNewInstance(args))
            return;

        var appBuilder = InfiniFrameBlazorAppBuilder.CreateDefault(args);
        appBuilder.Services.AddApplicationServices();
        appBuilder.RootComponents.Add<App>("app");


        bool startHidden = args.Contains("--hidden") && !args.Contains("--toggle-ui");
        appBuilder.ConfigureAndRunWindow(startHidden);
    }
}