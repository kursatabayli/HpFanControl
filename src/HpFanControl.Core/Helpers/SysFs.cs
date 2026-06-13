using System.Buffers.Text;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HpFanControl.Core.Helpers;

public sealed partial class SysFs
{
    public static ILogger? Logger { get; set; }

    public static int ParseInt(ReadOnlySpan<byte> buffer)
    {
        var trimmed = TrimSpan(buffer);

        if (Utf8Parser.TryParse(trimmed, out int result, out _))
        {
            return result;
        }

        return 0;
    }

    public static int ReadInt(ref FileStream? stream, string path, Span<byte> buffer)
    {
        try
        {
            EnsureStreamOpen(ref stream, path, FileAccess.Read);

            if (stream == null) return 0;

            stream.Seek(0, SeekOrigin.Begin);

            int bytesRead = stream.Read(buffer);

            if (bytesRead > 0)
            {
                return ParseInt(buffer[..bytesRead]);
            }
        }
        catch (IOException ex)
        {
            if (Logger != null) LogReadError(Logger, ex, path);
            ResetStream(ref stream);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (Logger != null) LogReadError(Logger, ex, path);
            ResetStream(ref stream);
        }
        return 0;
    }

    public static void WriteBytes(ref FileStream? stream, string path, ReadOnlySpan<byte> data)
    {
        try
        {
            EnsureStreamOpen(ref stream, path, FileAccess.Write);

            if (stream == null) return;

            stream.Seek(0, SeekOrigin.Begin);

            stream.Write(data);
            stream.Flush();
        }
        catch (IOException ex)
        {
            if (Logger != null) LogWriteError(Logger, ex, path);
            ResetStream(ref stream);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (Logger != null) LogWriteError(Logger, ex, path);
            ResetStream(ref stream);
        }
    }

    public static bool CheckContentEquals(ref FileStream? stream, string path, ReadOnlySpan<byte> expected, Span<byte> buffer)
    {
        try
        {
            EnsureStreamOpen(ref stream, path, FileAccess.Read);

            if (stream == null) return false;

            stream.Seek(0, SeekOrigin.Begin);
            int bytesRead = stream.Read(buffer);

            if (bytesRead < expected.Length) return false;

            var fileContent = buffer[..bytesRead];

            return fileContent.IndexOf(expected) >= 0;
        }
        catch (IOException ex)
        {
            if (Logger != null) LogCheckContentError(Logger, ex, path);
            ResetStream(ref stream);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            if (Logger != null) LogCheckContentError(Logger, ex, path);
            ResetStream(ref stream);
            return false;
        }
    }

    public static bool CheckContentEquals(ref FileStream? stream, string path, string expected, Span<byte> buffer)
    {
        Span<byte> expectedBytes = stackalloc byte[Encoding.ASCII.GetByteCount(expected)];
        Encoding.ASCII.GetBytes(expected, expectedBytes);

        return CheckContentEquals(ref stream, path, expectedBytes, buffer);
    }

    #region Helper Methods

    private static void EnsureStreamOpen(ref FileStream? stream, string path, FileAccess access)
    {
        if (stream != null) return;

        if (!File.Exists(path))
        {
            if (Logger != null) LogFileNotFound(Logger, path);
            return;
        }

        stream = new FileStream(path, FileMode.Open, access, FileShare.ReadWrite);
    }

    private static void ResetStream(ref FileStream? stream)
    {
        try
        {
            stream?.Dispose();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        stream = null;
    }

    public static ReadOnlySpan<byte> TrimSpan(ReadOnlySpan<byte> span)
    {
        int start = 0;
        while (start < span.Length && IsWhitespace(span[start]))
        {
            start++;
        }

        int end = span.Length - 1;
        while (end >= start && IsWhitespace(span[end]))
        {
            end--;
        }

        if (start > end) return [];

        return span.Slice(start, end - start + 1);
    }

    private static bool IsWhitespace(byte b)
    {
        return b == 32 || b == 9 || b == 10 || b == 13;
    }

    #endregion

    #region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to read integer from {Path}")]
    private static partial void LogReadError(ILogger logger, Exception ex, string path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to write bytes to {Path}")]
    private static partial void LogWriteError(ILogger logger, Exception ex, string path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to check content of {Path}")]
    private static partial void LogCheckContentError(ILogger logger, Exception ex, string path);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "File not found: {Path}")]
    private static partial void LogFileNotFound(ILogger logger, string path);

    #endregion
}