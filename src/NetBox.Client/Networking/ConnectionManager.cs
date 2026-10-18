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
    private TaskCompletionSource<GetChannelListResponse>? _getChannelListTcs;
    private TaskCompletionSource<JoinChannelResponse>? _joinChannelTcs;
    private TaskCompletionSource<LeaveChannelResponse>? _leaveChannelTcs;
    private TaskCompletionSource<CreateChannelResponse>? _createChannelTcs;
    private TaskCompletionSource<SendMessageResponse>? _sendMessageTcs;

    // use id, task completion source dict for concurrent requests, but for now we will assume only one request of each type at a time

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

    public async Task<GetChannelListResponse> GetChannelListAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new GetChannelListRequest();
        var bytes = req.ToMessage().Serialize();
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

        _getChannelListTcs = new TaskCompletionSource<GetChannelListResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => _getChannelListTcs.TrySetCanceled()))
        {
            return await _getChannelListTcs.Task;
        }
    }

    public async Task<JoinChannelResponse> JoinChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new JoinChannelRequest(channelName);
        var bytes = req.ToMessage().Serialize();
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

        _joinChannelTcs = new TaskCompletionSource<JoinChannelResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => _joinChannelTcs.TrySetCanceled()))
        {
            return await _joinChannelTcs.Task;
        }
    }

    public async Task<LeaveChannelResponse> LeaveChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new LeaveChannelRequest();
        var bytes = req.ToMessage().Serialize();
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

        _leaveChannelTcs = new TaskCompletionSource<LeaveChannelResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => _leaveChannelTcs.TrySetCanceled()))
        {
            return await _leaveChannelTcs.Task;
        }
    }

    public async Task<CreateChannelResponse> CreateChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new CreateChannelRequest(channelName);
        var bytes = req.ToMessage().Serialize();
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

        _createChannelTcs = new TaskCompletionSource<CreateChannelResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => _createChannelTcs.TrySetCanceled()))
        {
            return await _createChannelTcs.Task;
        }
    }

    public async Task<SendMessageResponse> SendChatMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        var req = new SendMessageRequest(message);
        var bytes = req.ToMessage().Serialize();
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

        _sendMessageTcs = new TaskCompletionSource<SendMessageResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => _sendMessageTcs.TrySetCanceled()))
        {
            return await _sendMessageTcs.Task;
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
                    // Handle response types and complete any pending TaskCompletionSources
                    try
                    {
                        if (msg.Command == Command.LOGIN)
                        {
                            if (LoginResponse.FromMessage(msg, out var loginResp))
                            {
                                _loginTcs?.TrySetResult(loginResp.ResponseCode);
                            }
                        }

                        if (msg.Command == Command.GET_CHANNEL_LIST)
                        {
                            if (GetChannelListResponse.FromMessage(msg, out var resp))
                            {
                                _getChannelListTcs?.TrySetResult(resp);
                            }
                        }

                        if (msg.Command == Command.JOIN_CHANNEL)
                        {
                            if (JoinChannelResponse.FromMessage(msg, out var resp))
                            {
                                _joinChannelTcs?.TrySetResult(resp);
                            }
                        }

                        if (msg.Command == Command.LEAVE_CHANNEL)
                        {
                            if (LeaveChannelResponse.FromMessage(msg, out var resp))
                            {
                                _leaveChannelTcs?.TrySetResult(resp);
                            }
                        }

                        if (msg.Command == Command.CREATE_CHANNEL)
                        {
                            if (CreateChannelResponse.FromMessage(msg, out var resp))
                            {
                                _createChannelTcs?.TrySetResult(resp);
                            }
                        }

                        if (msg.Command == Command.SEND_MESSAGE)
                        {
                            if (SendMessageResponse.FromMessage(msg, out var resp))
                            {
                                _sendMessageTcs?.TrySetResult(resp);
                            }
                        }
                    }
                    catch { }

                    // dispatch all messages to dispatcher (e.g., ChatMessage server events)
                    await _dispatcher.DispatchAsync(msg);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }
}
