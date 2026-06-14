namespace HpFanControl.Core.Helpers;

internal static class LinuxSysFsContracts
{
    public const string HwmonBaseDir = "/sys/class/hwmon";
    public const string PciDevicesDir = "/sys/bus/pci/devices";
    
    public const string FileName = "name";
    public const string FileTemp1Input = "temp1_input";
    
    public const string FilePwm1Enable = "pwm1_enable";
    public const string FilePwm1 = "pwm1";
    public const string FilePwm2 = "pwm2";
    public const string FileFan1Input = "fan1_input";
    public const string FileFan2Input = "fan2_input";

    public const string FileVendor = "vendor";
    public const string FileRuntimeStatus = "power/runtime_status";
}