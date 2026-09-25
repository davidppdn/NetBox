using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Requests;

/// <summary>
/// Class: LoginRequest
/// Description: Represents a login request from the client
/// Structure:
/// - LOGIN command
/// - No additional headers
/// - payload: username
/// 
/// See LoginCommand.md
/// </summary>
public class LoginRequest
{
    public string Username { get; }

    public LoginRequest(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        Username = username;
    }

    public Message ToMessage() 
        => new Message(Command.LOGIN, new Header(), Username);

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out LoginRequest? loginRequest)
    {
        loginRequest = null;

        if (message.Command != Command.LOGIN)
        {
            throw new InvalidDataException($"Command is not login: {message.Command}");
        }

        if (message.Header.Fields.Count != 0)
        {
            throw new InvalidDataException($"Extra headers found");
        }

        var username = message.Payload;
        if (username == null)
        {
            throw new InvalidDataException("Missing payload");
        }

        loginRequest = new LoginRequest(username);
        return true;
    }
}
