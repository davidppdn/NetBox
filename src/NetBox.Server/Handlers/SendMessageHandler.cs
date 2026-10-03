using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;

namespace NetBox.Server.Handlers;

internal class SendMessageHandler
{
    internal async Task Handle(Message message, ClientSession clientSession)
    {
        if (!SendMessageRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Send message command but not valid send message request");
        }

        Console.WriteLine($"[SendMessageHandler] Received send message request from {clientSession.Client.Client.RemoteEndPoint} with message: {request.Message}");

        var response = new SendMessageResponse(SendMessageResponseCode.SUCCESS);

        await clientSession.SendMessage(response);
    }
}
