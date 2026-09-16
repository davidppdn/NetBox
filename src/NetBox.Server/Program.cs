using System.Net;
using System.Net.Sockets;
using System.Text;
using NetBox.Shared.Protocols;

public static class Server
{
    private static readonly IPAddress IpAddress = IPAddress.Loopback;
    private static readonly int Port = 5000;

    public static async Task Main(string[] args)
    {
        var ipEndPoint = new IPEndPoint(IpAddress, Port);
        TcpListener listener = new TcpListener(ipEndPoint);

        try
        {
            listener.Start();

            Console.WriteLine($"Listening on {IpAddress}:{Port}");

            while (true)
            {
                // await pauses this method until a client connects without blocking the thread,
                // allowing the thread to do other work while waiting.
                TcpClient handler = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");
                // Start handling this connection without waiting for it to finish,
                // allowing the server to continue accepting new connections.
                _ = HandleConnection(handler);
            }            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }

    // This async method can pause at await without blocking the thread,
    // allowing other work to run while waiting for I/O.
    private static async Task HandleConnection(TcpClient handler)
    {
        await using NetworkStream stream = handler.GetStream();
        byte[] buffer = new byte[1024];
        var parser = new MessageParser();
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected.");
                break;
            }
            var receivedMessages = parser.ParseBytes(buffer, bytesRead);
            foreach (var message in receivedMessages)
            {
                Console.WriteLine($"Received message: {message.Content}");
            }
        }
    }
}
