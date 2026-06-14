using System.Net.Http.Headers;
using System.Reflection;
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

        services.AddHttpClient("GitHubApi", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/repos/kursatabayli/HpFanControl/");

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HpFanControl", currentVersion));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        });

        services.AddSingleton<PreferencesService>();
        services.AddSingleton<UpdateService>();

        services.AddMudServices();

        return services;
    }
}