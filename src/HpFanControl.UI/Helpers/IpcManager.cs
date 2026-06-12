using System.IO.Pipes;

namespace HpFanControl.UI.Helpers;

internal static class IpcManager
{
    private static readonly string PipeName = "HpFanControlPipe_" + Environment.UserName;

    public static bool TrySendMessage(string message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(200); 
            using var writer = new StreamWriter(client);
            writer.WriteLine(message);
            writer.Flush();
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void StartServer(Action<string> onMessageReceived)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync().ConfigureAwait(false);

                    using var reader = new StreamReader(server);

                    string? message = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(message))
                        onMessageReceived(message);
                }
                catch (IOException)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
        });
    }
}