using System.Runtime.InteropServices;

namespace HpFanControl.Core.Interop;

#pragma warning disable CA5392
public static partial class NvmlNative
{
    private const string LibName = "libnvidia-ml.so.1";

    [LibraryImport(LibName)] internal static partial int nvmlInit();
    [LibraryImport(LibName)] internal static partial int nvmlShutdown();
    [LibraryImport(LibName)] internal static partial int nvmlDeviceGetHandleByIndex(uint index, out IntPtr device);
    [LibraryImport(LibName)] internal static partial int nvmlDeviceGetTemperature(IntPtr device, int sensorType, ref uint temp);
}