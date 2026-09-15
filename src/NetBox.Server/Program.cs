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

            // Waits asynchronously until a client establishes a TCP connection
            using TcpClient handler = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = handler.GetStream();

            Console.WriteLine("Client connected.");

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
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }
}
