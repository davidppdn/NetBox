using NetBox.Server.Models;
using NetBox.Server.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;

namespace NetBox.Server.Handlers;

internal class LoginHandler
{
    private readonly ISessionManager _sessionManager;

    public LoginHandler(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    internal async Task Handle(Message message, ClientSession clientSession)
    {
        if (!LoginRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Login command but not valid login request");
        }

        Console.WriteLine($"[LoginHandler] Received login request from {clientSession.Client.Client.RemoteEndPoint} with username: {request.Username}");

        var success = _sessionManager.TryAuthenticate(clientSession, request.Username);
        if (!success)
        {
            var fail = new LoginResponse(LoginResponseCode.FAIL);
            await clientSession.SendMessage(fail);
            return;
        }

        var response = new LoginResponse(LoginResponseCode.SUCCESS);
        await clientSession.SendMessage(response);
    }
}
