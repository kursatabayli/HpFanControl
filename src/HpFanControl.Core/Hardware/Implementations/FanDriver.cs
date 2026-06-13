using System.Buffers.Text;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Helpers;
using HpFanControl.Core.Models;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class FanDriver(ILogger<FanDriver> logger) : IFanDriver
{
    private static readonly byte[] HwmonName = "hp"u8.ToArray();
    private const string FilePwmEnable = "pwm1_enable";
    private const string FileCpuPwm = "pwm1";
    private const string FileGpuPwm = "pwm2";
    private const string FileCpuFanInput = "fan1_input";
    private const string FileGpuFanInput = "fan2_input";

    private FileStream? _streamCpuPwm;
    private FileStream? _streamGpuPwm;
    private FileStream? _streamCpuInput;
    private FileStream? _streamGpuInput;

    private string? _detectedPath;

    private readonly byte[] _readBuffer = new byte[64];

    public (int CpuRpm, int GpuRpm) GetRpms()
    {
        EnsurePath();
        if (_detectedPath == null) return (0, 0);

        int cpu = SysFs.ReadInt(ref _streamCpuInput, Path.Combine(_detectedPath, FileCpuFanInput), _readBuffer);
        int gpu = SysFs.ReadInt(ref _streamGpuInput, Path.Combine(_detectedPath, FileGpuFanInput), _readBuffer);

        return (cpu, gpu);
    }

    public void SetMode(FanMode mode)
    {
        EnsurePath();
        if (_detectedPath == null) return;


        if (mode != FanMode.Manual)
        {
            ClosePwmStreams();
        }

        string path = Path.Combine(_detectedPath, FilePwmEnable);

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);

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

        if (_detectedPath == null)
        {
            LogSetSpeedFailedPathNotFound();
            return;
        }

        int safePwm = Math.Clamp(pwm, 0, 255);


        Span<byte> buffer = stackalloc byte[4];

        if (!Utf8Formatter.TryFormat(safePwm, buffer, out int bytesWritten))
            return;

        string fileName = isGpu ? FileGpuPwm : FileCpuPwm;
        ref FileStream? stream = ref isGpu ? ref _streamGpuPwm : ref _streamCpuPwm;

        SysFs.WriteBytes(ref stream, Path.Combine(_detectedPath, fileName), buffer.Slice(0, bytesWritten));
    }

    private void EnsurePath()
    {
        if (_detectedPath != null) return;

        var baseDir = "/sys/class/hwmon";
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(baseDir))
            {
                string pwmPath = Path.Combine(dir, FilePwmEnable);
                if (!File.Exists(pwmPath)) continue;

                var namePath = Path.Combine(dir, "name");
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
                    _detectedPath = dir;
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
            if (_detectedPath != null)
            {
                SetMode(FanMode.Auto);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        _streamCpuInput?.Dispose();
        _streamGpuInput?.Dispose();

        ClosePwmStreams();

        LogDriverDisposed();

        GC.SuppressFinalize(this);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "SetSpeed failed: HP Driver path not found.")]
    private partial void LogSetSpeedFailedPathNotFound();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "HP Fan Control compatible hardware not found.")]
    private partial void LogHardwareNotFound();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error while probing hardware.")]
    private partial void LogProbingError(Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "HP Fan Driver disposed.")]
    private partial void LogDriverDisposed();
}