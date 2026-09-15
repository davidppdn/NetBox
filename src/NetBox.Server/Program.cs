using System.Net;
using System.Net.Sockets;

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
