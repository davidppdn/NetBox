using NetBox.Shared.Protocols;
using System.Net.Sockets;

public static class Client
{
    public static async Task Main(string[] args)
    {
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        Console.WriteLine("Connected to server.");

        using var stream = client.GetStream();

        while (true)
        {
            Console.Write("Enter a message to send (or 'exit' to quit): ");
            string message = Console.ReadLine();
            
            if (message.ToLower() == "exit")
                break;

            Message netMessage = new(message);
            byte[] data = netMessage.ToBytes();
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
}