using NetBox.Server.Handlers;
using NetBox.Server.Interfaces;
using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using System.Net;
using System.Net.Sockets;
using System.Xml.XPath;

public static class Server
{
    private static readonly IPAddress IpAddress = IPAddress.Loopback;
    private static readonly int Port = 5000;
    private static readonly ISessionManager SessionManager = new SessionManager();
    private static readonly ChannelManager ChannelManager = new ChannelManager();

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
        var factory = new CommandHandlerFactory(SessionManager, ChannelManager);
        var delegator = new CommandDelegator(factory);
        string? currentChannel = null;

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
                    var result = await delegator.Handle(message, clientSession);
                    if (result != null && result is NetBox.Shared.ServerEvents.ChatMessage chat)
                    {
                        await BroadcastMessage(chat, clientSession);
                    }

                    if (result != null && result is NetBox.Shared.ServerEvents.JoinMessage join)
                    {
                        Console.WriteLine($"User {clientSession.Username} joined channel {clientSession.Channel}");
                        currentChannel = clientSession.Channel;
                        await BroadcastMessage(join, clientSession);
                    }

                    if (result != null && result is NetBox.Shared.ServerEvents.LeaveMessage leave)
                    {
                        Console.WriteLine($"User {clientSession.Username} left channel {clientSession.Channel}");
                        await BroadcastMessage(leave, clientSession, currentChannel);
                        currentChannel = null;
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
                // If client was in a channel, remove from that channel
                if (clientSession.Channel != null)
                {
                    if (ChannelManager.TryGetChannel(clientSession.Channel, out var channel))
                    {
                        channel.RemoveMember(clientSession);
                    }
                }

                SessionManager.Remove(clientSession);

                clientSession.Client.Close();
            }
    }

    private static async Task BroadcastMessage(IMessage message, ClientSession sender, string? currentChannel = null)
    {
        var channelName = sender.Channel ?? currentChannel;
        if (channelName == null || channelName == "")
        {
            return;
        }

        if (!ChannelManager.TryGetChannel(channelName, out var channel))
        {
            return;
        }

        byte[] messageBytes = message.ToMessage().Serialize();

        var members = channel.GetMembers();
        foreach (var client in members)
        {
            if (client.Username == null) continue;
            if (client == sender) continue;

            try
            {
                var stream = client.Client.GetStream();
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch
            {
            }
        }
    }
}
