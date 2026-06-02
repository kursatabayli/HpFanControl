using HpFanControl.Core.Hardware.Implementations;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Services.Implementations;
using HpFanControl.Core.Services.Interfaces;
using HpFanControl.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace HpFanControl.UI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IFanControllerService, FanControllerService>();
        services.AddSingleton<ICpuSensor, CpuSensor>();
        services.AddSingleton<IGpuSensor, GpuSensor>();
        services.AddSingleton<IFanDriver, FanDriver>();

        services.AddKeyedSingleton<IGpuProvider, NvidiaGpuProvider>("Discrete");
        services.AddKeyedSingleton<IGpuProvider, IntegratedGpuProvider>("Integrated");

        services.AddSingleton<WindowActionService>();
        services.AddMudServices();

        return services;
    }
}