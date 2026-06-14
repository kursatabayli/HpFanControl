using System.Text;
using HpFanControl.Core.Hardware.Interfaces;
using HpFanControl.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace HpFanControl.Core.Hardware.Implementations;

public sealed partial class IntegratedGpuProvider(ILogger<IntegratedGpuProvider> logger) : IGpuProvider
{
  private readonly ILogger<IntegratedGpuProvider> _logger = logger;
  private string? _tempPath;
  private FileStream? _stream;
  private readonly byte[] _buffer = new byte[16];

  private static readonly byte[][] Drivers = ["amdgpu"u8.ToArray(), "i915"u8.ToArray()];

  public string Name => "Integrated GPU";
  public bool IsAvailable => _tempPath != null;
  public bool IsActive => true;

  public void Initialize()
  {
    if (_tempPath != null) return;

    if (!Directory.Exists(LinuxSysFsContracts.HwmonBaseDir)) return;

    Span<byte> nameBuffer = stackalloc byte[64];

    foreach (var dir in Directory.EnumerateDirectories(LinuxSysFsContracts.HwmonBaseDir))
    {
      var potentialPath = Path.Combine(dir, LinuxSysFsContracts.FileTemp1Input);
      if (!File.Exists(potentialPath)) continue;

      var namePath = Path.Combine(dir, LinuxSysFsContracts.FileName);

      try
      {
        using var fs = File.OpenRead(namePath);
        int bytesRead = fs.Read(nameBuffer);

        var content = SysFs.TrimSpan(nameBuffer[..bytesRead]);

        foreach (var driver in Drivers)
        {
          if (content.SequenceEqual(driver))
          {
            _tempPath = potentialPath;

            if (_logger.IsEnabled(LogLevel.Information))
            {
              var driverStr = Encoding.UTF8.GetString(driver);
              LogIgpuFound(driverStr, _tempPath);
            }
            return;
          }
        }
      }
      catch (UnauthorizedAccessException)
      {
        continue;
      }
      catch (IOException)
      {
        continue;
      }
    }
  }

  public int GetTemperature()
  {
    if (_tempPath == null) return 0;

    return SysFs.ReadInt(ref _stream, _tempPath, _buffer) / 1000;
  }

  public void Dispose()
  {
    _stream?.Dispose();

    GC.SuppressFinalize(this);
  }

  #region Logging
  [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Integrated GPU Provider found: {Driver} at {Path}")]
  private partial void LogIgpuFound(string driver, string path);
  #endregion
}