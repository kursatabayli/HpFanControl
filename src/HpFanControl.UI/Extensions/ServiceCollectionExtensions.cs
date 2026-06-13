using HpFanControl.Core;
using HpFanControl.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace HpFanControl.UI.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddCoreServices();

        services.AddSingleton<WindowActionService>();
        services.AddMudServices();

        return services;
    }
}