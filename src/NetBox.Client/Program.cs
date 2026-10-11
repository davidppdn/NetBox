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

        var loginTcs = new TaskCompletionSource<LoginResponseCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<TaskCompletionSource<LoginResponseCode>> getLoginTcs = () => loginTcs;
        Action<TaskCompletionSource<LoginResponseCode>> setLoginTcs = (t) => loginTcs = t;

        var readTask = HandleReads(stream, cts.Token, getLoginTcs);
        var inputTask = HandleInput(stream, cts.Token, getLoginTcs, setLoginTcs);

        await Task.WhenAny(readTask, inputTask);
        
        cts.Cancel();

        await Task.WhenAll(readTask, inputTask);
    }

    private static async Task HandleInput(NetworkStream stream, CancellationToken cancellationToken, Func<TaskCompletionSource<LoginResponseCode>> getLoginTcs, Action<TaskCompletionSource<LoginResponseCode>> setLoginTcs)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Login with username:");
                string username = Console.ReadLine();
                if (username == null)
                    break;

                var loginRequest = new LoginRequest(username);

                byte[] data = loginRequest.ToMessage().Serialize();
                await stream.WriteAsync(data, 0, data.Length, cancellationToken);

                // Wait for login response (or cancellation)
                LoginResponseCode loginCode;
                try
                {
                    loginCode = await getLoginTcs().Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested, exit
                    break;
                }

                if (loginCode == LoginResponseCode.SUCCESS)
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

                    // Exit after successful session
                    break;
                }
                else
                {
                    Console.WriteLine($"Login failed: {loginCode}. Please try again.");
                    // Reset the awaiter so we can wait for the next login response
                    setLoginTcs(new TaskCompletionSource<LoginResponseCode>(TaskCreationOptions.RunContinuationsAsynchronously));
                    // Continue loop to prompt for credentials again
                }
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

    private static async Task HandleReads(NetworkStream stream, CancellationToken cancellationToken, Func<TaskCompletionSource<LoginResponseCode>> getLoginTcs)
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
                            getLoginTcs().TrySetResult(loginResponse.ResponseCode);
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