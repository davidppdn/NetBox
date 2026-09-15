using System.Net.Sockets;

public static class Client
{
    public static async Task Main(string[] args)
    {
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);
    }
}