using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;

namespace NetBox.Server.Handlers;

internal class LoginHandler
{
    internal async Task Handle(Message message, ClientSession clientSession)
    {
        if (!LoginRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Login command but not valid login request");
        }

        Console.WriteLine($"[LoginHandler] Received login request from {clientSession.Client.Client.RemoteEndPoint} with username: {request.Username}");
        clientSession.SetUsername(request.Username);

        var response = new LoginResponse(LoginResponseCode.SUCCESS);

        await clientSession.SendMessage(response);
    }
}
