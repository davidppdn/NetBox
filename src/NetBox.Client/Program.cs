using NetBox.Shared.Protocols;
using System.Net.Sockets;

public static class Client
{
    public static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 5000);

        Console.WriteLine("Connected to server.");

        using var stream = client.GetStream();

        var readTask = HandleReads(stream, cts.Token);
        var inputTask = HandleInput(stream, cts.Token);

        await Task.WhenAny(readTask, inputTask);
        
        cts.Cancel();

        await Task.WhenAll(readTask, inputTask);
    }

    private static async Task HandleInput(NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("Enter a message to send (or 'exit' to quit): ");
                string message = Console.ReadLine();

                if (message.ToLower() == "exit")
                    break;

                Message netMessage = new(message);
                byte[] data = netMessage.ToBytes();
                await stream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Cancel occurred.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private static async Task HandleReads(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024];
        var parser = new MessageParser();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
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
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Cancel occurred.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading from server: {ex.Message}");
        }

    }
}