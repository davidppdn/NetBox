using NetBox.Shared.Protocols;

namespace NetBox.Client.Networking;

public class MessageDispatcher
{
    public event Func<Message, Task>? MessageReceived;

    public async Task DispatchAsync(Message message)
    {
        var handler = MessageReceived;
        if (handler != null)
        {
            await handler.Invoke(message);
        }
    }
}
