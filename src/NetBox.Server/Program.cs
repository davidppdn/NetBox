using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using System.Net;
using System.Net.Sockets;

public static class Server
{
    private static readonly IPAddress IpAddress = IPAddress.Loopback;
    private static readonly int Port = 5000;

    private static readonly List<ClientSession> ConnectedClients = [];
    private static readonly object ClientsLock = new();

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

                lock (ClientsLock)
                {
                    ConnectedClients.Add(clientSession);
                }

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
                    if (message == null) continue;

                    if (message.Command == Command.LOGIN)
                    {
                        if (!LoginRequest.FromMessage(message, out var request))
                        {
                            throw new InvalidDataException("Login request command but not login request");
                        }

                        Console.WriteLine($"Login request from: {request.Username}");
                        clientSession.SetUsername(request.Username);

                        var loginResponse = new LoginResponse(LoginResponseCode.SUCCESS);
                        await clientSession.SendMessage(loginResponse);
                    }

                    if (message.Command == Command.SEND_MESSAGE)
                    {
                        if (!SendMessageRequest.FromMessage(message, out var request))
                        {
                            throw new InvalidDataException("Send message request command but not send message request");
                        }
                        Console.WriteLine($"Message from {clientSession.Username}: {request.Message}");
                        
                        var sendMessageResponse = new SendMessageResponse(SendMessageResponseCode.SUCCESS);
                        await clientSession.SendMessage(sendMessageResponse);
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
            lock (ClientsLock)
            {
                ConnectedClients.Remove(clientSession);
            }

            clientSession.Client.Close();
        }
    }

    //private static async Task BroadcastMessage(string message)
    //{
    //    byte[] messageBytes = new Message(message).ToBytes();

    //    var clientListCopy = new List<TcpClient>();
       
    //    lock (ClientsLock)
    //    {
    //        clientListCopy.AddRange(ConnectedClients);
    //    }

    //    foreach (var client in clientListCopy)
    //    {
    //        var stream = client.GetStream();
    //        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
    //    }
    //}
}
