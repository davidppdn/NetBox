using NetBox.Server;
using NetBox.Server.Handlers;
using NetBox.Server.Interfaces;
using NetBox.Server.Models;
using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Net;
using System.Net.Sockets;

public static class Server
{
    private static readonly IPAddress IpAddress = IPAddress.Loopback;
    private static readonly int Port = 5000;
    private static readonly ISessionManager SessionManager = new SessionManager();

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
                var clientSession = new ClientSession(handler);

                SessionManager.Add(clientSession);

                Console.WriteLine("Client connected.");
                _ = HandleConnection(clientSession);
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

    private static async Task HandleConnection(ClientSession clientSession)
    {
        var loginHandler = new LoginHandler(SessionManager);
        var sendMessageHandler = new SendMessageHandler();

        try
        {
            await using NetworkStream stream = clientSession.Client.GetStream();
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
                    if (message.Command == Command.LOGIN)
                    {
                        await loginHandler.Handle(message, clientSession);
                    }

                    if (message.Command == Command.SEND_MESSAGE)
                    {
                        var chatMessage = await sendMessageHandler.Handle(message, clientSession);
                        if (chatMessage != null)
                        {
                            await BroadcastMessage(chatMessage, clientSession);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while handling connection: {ex.Message}");
        }
            finally
            {
                SessionManager.Remove(clientSession);

                clientSession.Client.Close();
            }
    }

    private static async Task BroadcastMessage(IMessage message, ClientSession sender)
    {
        byte[] messageBytes = message.ToMessage().Serialize();

        var clientSessionCopy = SessionManager.GetAll();

        foreach (var client in clientSessionCopy)
        {
            if (client.Username == null) continue;
            if (client == sender) continue;

            var stream = client.Client.GetStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }
    }
}
