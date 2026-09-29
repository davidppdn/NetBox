using System.Net.Sockets;

namespace NetBox.Server.Models;

/// <summary>
/// A state record representing a client session.
/// Currently, if a username is not null, the session is considered authenticated.
/// </summary>
/// <param name="Client"></param>
/// <param name="Username"></param>
internal record ClientSession(TcpClient Client)
{
    public string? Username { get; private set; }

    public void SetUsername(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        Username = username;
    }
}