using System.Net;
using System.Net.Sockets;
using System.Text;
using NetBox.Shared.Protocols;

public static class Server
{
    // The IP address and port the server will listen on.
    private static readonly IPAddress IpAddress = IPAddress.Loopback;
    private static readonly int Port = 5000;

    // List for keeping track of connected clients. Needs resource lock since it is shared
    // between multiple concurrent connection handlers.
    private static readonly List<TcpClient> ConnectedClients = new List<TcpClient>();
    private static readonly object ClientsLock = new object();

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
                TcpClient handler = await listener.AcceptTcpClientAsync();

                lock (ClientsLock)
                {
                    ConnectedClients.Add(handler);
                }

                Console.WriteLine("Client connected.");
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

    private static async Task HandleConnection(TcpClient handler)
    {
        try
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
                    await BroadcastMessage(message.Content);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while handling connection: {ex.Message}");
        }
        finally
        {
            lock (ClientsLock)
            {
                ConnectedClients.Remove(handler);
            }

            handler.Close();
        }
    }

    private static async Task BroadcastMessage(string message)
    {
        byte[] messageBytes = new Message(message).ToBytes();

        var clientListCopy = new List<TcpClient>();
       
        lock (ClientsLock)
        {
            clientListCopy.AddRange(ConnectedClients);
        }

        foreach (var client in clientListCopy)
        {
            var stream = client.GetStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }
    }
}
