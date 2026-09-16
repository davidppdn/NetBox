using NetBox.Shared.Protocols;
using System.Net.Sockets;

public static class Client
{
    public static async Task Main(string[] args)
    {
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        Console.WriteLine("Connected to server.");

        var readTask = HandleReads(client);
        var inputTask = HandleInput(client);

        await inputTask;
    }

    private static async Task HandleInput(TcpClient client)
    {
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

    private static async Task HandleReads(TcpClient client)
    {
        using var stream = client.GetStream();

        byte[] buffer = new byte[1024];
        var parser = new MessageParser();

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Server disconnected.");
                break;
            }
            List<Message> messages = parser.ParseBytes(buffer, bytesRead);
            
            foreach (var msg in messages)
            {
                Console.WriteLine($"Received: {msg.Content}");
            }
        }
    }
}