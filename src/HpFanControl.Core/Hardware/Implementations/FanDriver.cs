using System.Buffers.Text;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Helpers;
using HpFanControl.Core.Models;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class FanDriver(ILogger<FanDriver> logger) : IFanDriver
{
    private static readonly byte[] HwmonName = "hp"u8.ToArray();

    private FileStream? _streamCpuPwm;
    private FileStream? _streamGpuPwm;
    private FileStream? _streamCpuInput;
    private FileStream? _streamGpuInput;

    private DevicePaths? _paths;

    private sealed record DevicePaths(
        string BaseDir,
        string CpuInput,
        string GpuInput,
        string Pwm1Enable,
        string CpuPwm,
        string GpuPwm
    );

    private readonly byte[] _readBuffer = new byte[64];

    public (int CpuRpm, int GpuRpm) GetRpms()
    {
        EnsurePath();
        if (_paths == null) return (0, 0);

        int cpu = SysFs.ReadInt(ref _streamCpuInput, _paths.CpuInput, _readBuffer);
        int gpu = SysFs.ReadInt(ref _streamGpuInput, _paths.GpuInput, _readBuffer);

        return (cpu, gpu);
    }

    public void SetMode(FanMode mode)
    {
        EnsurePath();
        if (_paths == null) return;


        if (mode != FanMode.Manual)
            ClosePwmStreams();

        using var fs = new FileStream(_paths.Pwm1Enable, FileMode.Open, FileAccess.Write);

        byte value = mode switch
        {
            FanMode.Auto => (byte)'2',
            FanMode.Manual => (byte)'1',
            FanMode.Max => (byte)'0',
            _ => (byte)'2'
        };

        fs.WriteByte(value);
    }

    public void SetSpeed(bool isGpu, int pwm)
    {
        EnsurePath();

        if (_paths == null)
        {
            LogSetSpeedFailedPathNotFound();
            return;
        }

        int safePwm = Math.Clamp(pwm, 0, 255);


        Span<byte> buffer = stackalloc byte[4];

        if (!Utf8Formatter.TryFormat(safePwm, buffer, out int bytesWritten))
            return;

        string targetPath = isGpu ? _paths.GpuPwm : _paths.CpuPwm;
        ref FileStream? stream = ref isGpu ? ref _streamGpuPwm : ref _streamCpuPwm;

        SysFs.WriteBytes(ref stream, targetPath, buffer[..bytesWritten]);
    }

    private void EnsurePath()
    {
        if (_paths != null) return;

        var baseDir = LinuxSysFsContracts.HwmonBaseDir;
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(baseDir))
            {
                string pwmPath = Path.Combine(dir, LinuxSysFsContracts.FilePwm1Enable);
                if (!File.Exists(pwmPath)) continue;

                var namePath = Path.Combine(dir, LinuxSysFsContracts.FileName);
                FileStream? tempStream = null;
                bool isHp = false;
                try
                {
                    isHp = SysFs.CheckContentEquals(ref tempStream, namePath, HwmonName, _readBuffer);
                }
                finally
                {
                    tempStream?.Dispose();
                }

                if (isHp)
                {
                    _paths = new DevicePaths(
                        BaseDir: dir,
                        CpuInput: Path.Combine(dir, LinuxSysFsContracts.FileFan1Input),
                        GpuInput: Path.Combine(dir, LinuxSysFsContracts.FileFan2Input),
                        Pwm1Enable: Path.Combine(dir, LinuxSysFsContracts.FilePwm1Enable),
                        CpuPwm: Path.Combine(dir, LinuxSysFsContracts.FilePwm1),
                        GpuPwm: Path.Combine(dir, LinuxSysFsContracts.FilePwm2)
                    );

                    return;
                }
            }
            LogHardwareNotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            LogProbingError(ex);
        }
        catch (IOException ex)
        {
            LogProbingError(ex);
        }
    }

    private void ClosePwmStreams()
    {
        _streamCpuPwm?.Dispose();
        _streamCpuPwm = null;
        _streamGpuPwm?.Dispose();
        _streamGpuPwm = null;
    }

    public void Dispose()
    {
        try
        {
            if (_paths != null)
                SetMode(FanMode.Auto);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        _streamCpuInput?.Dispose();
        _streamGpuInput?.Dispose();

        ClosePwmStreams();

        LogDriverDisposed();

        GC.SuppressFinalize(this);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "SetSpeed failed: HP Driver path not found.")]
    private partial void LogSetSpeedFailedPathNotFound();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "HP Fan Control compatible hardware not found.")]
    private partial void LogHardwareNotFound();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error while probing hardware.")]
    private partial void LogProbingError(Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "HP Fan Driver disposed.")]
    private partial void LogDriverDisposed();
    #endregion
}