using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Helpers;
using HpFanControl.Core.Interop;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class NvidiaGpuProvider(ILogger<NvidiaGpuProvider> logger) : IGpuProvider
{
  private static readonly byte[] StatusActive = "active"u8.ToArray();
  private static readonly byte[] VendorNvidia = "0x10de"u8.ToArray();

  private string? _statusPath;
  private bool? _isNvmlAvailable;
  private IntPtr _nvmlDeviceHandle = IntPtr.Zero;

  private FileStream? _streamStatus;
  private readonly byte[] _buffer = new byte[64];

  public string Name => "Nvidia Discrete GPU";
  public bool IsAvailable => _statusPath != null;

  public bool IsActive
  {
    get
    {
      if (_statusPath == null) return false;
      return SysFs.CheckContentEquals(ref _streamStatus, _statusPath, StatusActive, _buffer);
    }
  }

  public void Initialize()
  {
    if (_statusPath != null) return;

    const string pciRoot = "/sys/bus/pci/devices";
    if (!Directory.Exists(pciRoot)) return;

    foreach (var dir in Directory.EnumerateDirectories(pciRoot))
    {
      var vPath = Path.Combine(dir, "vendor");
      FileStream? fs = null;
      try
      {
        if (SysFs.CheckContentEquals(ref fs, vPath, VendorNvidia, _buffer))
        {
          _statusPath = Path.Combine(dir, "power/runtime_status");

          if (logger.IsEnabled(LogLevel.Information))
             LogHardwareFound(_statusPath);

          break;
        }
      }
      catch (UnauthorizedAccessException) { }
      catch (IOException) { }
      finally { fs?.Dispose(); }
    }
  }

  public int GetTemperature()
  {
    if (!IsAvailable || !IsActive) return 0;

    return ReadNvmlTemp();
  }

  private int ReadNvmlTemp()
  {
    if (_isNvmlAvailable == false) return 0;

    try
    {
      if (_isNvmlAvailable == null)
      {
        int initResult = NvmlNative.nvmlInit();
        if (initResult == 0)
        {
          _isNvmlAvailable = true;
          NvmlNative.nvmlDeviceGetHandleByIndex(0, out _nvmlDeviceHandle);
          LogNvmlInitSuccess();
        }
        else
        {
          LogNvmlInitFailed(initResult);
          _isNvmlAvailable = false;
          return 0;
        }
      }

      uint temp = 0;
      if (NvmlNative.nvmlDeviceGetTemperature(_nvmlDeviceHandle, 0, ref temp) == 0)
      {
        return (int)temp;
      }
    }
    catch (DllNotFoundException)
    {
      LogNvmlMissing();
      _isNvmlAvailable = false;
    }
    catch (EntryPointNotFoundException ex)
    {
      LogReadTempFailed(ex);
    }
    catch (ExternalException ex)
    {
      LogReadTempFailed(ex);
    }

    return 0;
  }

  public void Dispose()
  {
    _streamStatus?.Dispose();

    if (_isNvmlAvailable == true)
    {
      try 
      { 
          _ = NvmlNative.nvmlShutdown(); 
      } 
      catch (DllNotFoundException) { }
      catch (EntryPointNotFoundException) { }
    }
  }

  [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Nvidia Hardware found. PM Path: {Path}")]
  private partial void LogHardwareFound(string path);

  [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "NVML Library Initialized.")]
  private partial void LogNvmlInitSuccess();

  [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "NVML Init failed. Code: {Code}")]
  private partial void LogNvmlInitFailed(int code);

  [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Nvidia drivers not installed (libnvidia-ml.so.1 missing).")]
  private partial void LogNvmlMissing();

  [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Failed to read Nvidia temp.")]
  private partial void LogReadTempFailed(Exception ex);
}