using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetBox.Client.Networking;

public class ConnectionManager
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly MessageParser _parser = new();
    private readonly MessageDispatcher _dispatcher;

    private TaskCompletionSource<LoginResponseCode>? _loginTcs;

    public ConnectionManager(MessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public bool IsConnected => _client?.Connected ?? false;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);
        _stream = _client.GetStream();

        _ = Task.Run(() => ReadLoopAsync(cancellationToken), cancellationToken);
    }

    public async Task<LoginResponseCode> LoginAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new LoginRequest(username);
        var data = req.ToMessage().Serialize();
        await _stream.WriteAsync(data, 0, data.Length, cancellationToken);

        _loginTcs = new TaskCompletionSource<LoginResponseCode>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (cancellationToken.Register(() => _loginTcs.TrySetCanceled()))
        {
            return await _loginTcs.Task;
        }
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (read == 0) break;

                var messages = _parser.ParseBytes(buffer, read);
                foreach (var msg in messages)
                {
                    if (msg.Command == Command.LOGIN)
                    {
                        if (LoginResponse.FromMessage(msg, out var loginResp))
                        {
                            _loginTcs?.TrySetResult(loginResp.ResponseCode);
                        }
                    }

                    // dispatch all messages to dispatcher
                    await _dispatcher.DispatchAsync(msg);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }
}
