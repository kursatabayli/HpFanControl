using System.Reflection;

namespace HpFanControl.Core.Helpers;

public static class AppInfo
{
    public static string Name => Assembly.GetEntryAssembly()?.GetName().Name 
        ?? throw new InvalidOperationException("Unable to determine application name. Entry assembly is null.");

    public static string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
}