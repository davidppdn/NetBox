using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.ServerEvents;

namespace NetBox.Server.Handlers;

internal class SendMessageHandler
{
    internal async Task<ChatMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!SendMessageRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Send message command but not valid send message request");
        }

        if (clientSession.Username == null)
        {
            Console.WriteLine($"[SendMessageHandler] Received send message request from {clientSession.Client.Client.RemoteEndPoint} but client is not logged in");
            var failResponse = new SendMessageResponse(SendMessageResponseCode.FAIL);
            await clientSession.SendMessage(failResponse);
            return null;
        }

        Console.WriteLine($"[SendMessageHandler] Received send message request from {clientSession.Client.Client.RemoteEndPoint} with message: {request.Message}");

        var response = new SendMessageResponse(SendMessageResponseCode.SUCCESS);
        var chatMessage = new ChatMessage(clientSession.Username, request.Message);

        await clientSession.SendMessage(response);

        return chatMessage;
    }
}
