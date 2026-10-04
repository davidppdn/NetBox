using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.ServerEvents;
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

        var loginTcs = new TaskCompletionSource<NetBox.Shared.Protocols.Enums.LoginResponseCode>(TaskCreationOptions.RunContinuationsAsynchronously);

        var readTask = HandleReads(stream, cts.Token, loginTcs);
        var inputTask = HandleInput(stream, cts.Token, loginTcs);

        await Task.WhenAny(readTask, inputTask);
        
        cts.Cancel();

        await Task.WhenAll(readTask, inputTask);
    }

    private static async Task HandleInput(NetworkStream stream, CancellationToken cancellationToken, TaskCompletionSource<NetBox.Shared.Protocols.Enums.LoginResponseCode> loginTcs)
    {
        try
        {
            Console.WriteLine("Login with username:");
            string username = Console.ReadLine();

            var loginRequest = new LoginRequest(username);

            byte[] data = loginRequest.ToMessage().Serialize();
            await stream.WriteAsync(data, 0, data.Length);
            // Wait for login response (or cancellation)
            var loginCode = await loginTcs.Task.WaitAsync(cancellationToken);

            if (loginCode == NetBox.Shared.Protocols.Enums.LoginResponseCode.SUCCESS)
            {
                Console.WriteLine("Login successful. You can now send messages. Type '/logout' to exit.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write("Send message: ");
                    var message = Console.ReadLine();
                    if (message == null)
                        break;

                    if (message.Trim().Equals("/logout", StringComparison.OrdinalIgnoreCase))
                        break;

                    var sendReq = new SendMessageRequest(message);
                    var sendData = sendReq.ToMessage().Serialize();
                    await stream.WriteAsync(sendData, 0, sendData.Length, cancellationToken);
                }
            }
            else
            {
                Console.WriteLine($"Login failed: {loginCode}");
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

    private static async Task HandleReads(NetworkStream stream, CancellationToken cancellationToken, TaskCompletionSource<NetBox.Shared.Protocols.Enums.LoginResponseCode> loginTcs)
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
                    if (msg.Command == Command.LOGIN)
                    {
                        if (LoginResponse.FromMessage(msg, out var loginResponse))
                        {
                            Console.WriteLine($"Login command: {loginResponse.ResponseCode}");
                            // Signal login result for input task
                            loginTcs.TrySetResult(loginResponse.ResponseCode);
                        }
                    }

                    if (msg.Command == Command.SEND_MESSAGE)
                    {
                        if (SendMessageResponse.FromMessage(msg, out var sendMessageResponse))
                        {
                            Console.WriteLine($"Server response: {sendMessageResponse.ResponseCode}");
                        }
                    }

                    if (msg.Command == Command.CHAT_MESSAGE)
                    {
                        if (ChatMessage.FromMessage(msg, out var chatMessage))
                        {
                            Console.WriteLine($"[CHAT] {chatMessage.Sender}: {chatMessage.Message}");
                        }
                    }
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