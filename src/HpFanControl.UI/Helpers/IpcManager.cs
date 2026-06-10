using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace HpFanControl.UI.Helpers;

public static class IpcManager
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
        catch (Exception)
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
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server);
                    string message = await reader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(message))
                    {
                        onMessageReceived(message);
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                }
            }
        });
    }
}