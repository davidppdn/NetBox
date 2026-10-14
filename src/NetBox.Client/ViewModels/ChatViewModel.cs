using NetBox.Client.Networking;
using NetBox.Shared.Protocols.Enums;
using System.Threading;

namespace NetBox.Client.ViewModels;

public class ChatViewModel
{
    private readonly ConnectionManager _connection;
    private readonly MessageDispatcher _dispatcher;

    public ChatViewModel(ConnectionManager connection, MessageDispatcher dispatcher)
    {
        _connection = connection;
        _dispatcher = dispatcher;
    }

    public bool IsConnected => _connection.IsConnected;

    public async Task<bool> LoginAsync(string username, CancellationToken cancellationToken = default)
    {
        // connect if not connected
        if (!_connection.IsConnected)
        {
            await _connection.ConnectAsync("127.0.0.1", 5000, cancellationToken);
        }

        var code = await _connection.LoginAsync(username, cancellationToken);
        return code == LoginResponseCode.SUCCESS;
    }
}
