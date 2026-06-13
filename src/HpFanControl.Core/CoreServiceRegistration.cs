using HpFanControl.Core.Hardware.Implementations;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Services.Implementations;
using HpFanControl.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HpFanControl.Core;

public static class CoreServiceRegistration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {

        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IFanControllerService, FanControllerService>();
        services.AddSingleton<ICpuSensor, CpuSensor>();
        services.AddSingleton<IGpuSensor, GpuSensor>();
        services.AddSingleton<IFanDriver, FanDriver>();

        services.AddKeyedSingleton<IGpuProvider, NvidiaGpuProvider>("Discrete");
        services.AddKeyedSingleton<IGpuProvider, IntegratedGpuProvider>("Integrated");

        return services;
    }
}