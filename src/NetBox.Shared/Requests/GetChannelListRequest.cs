using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Requests;

public class GetChannelListRequest : IMessage
{
    public GetChannelListRequest()
    {
    }

    public Message ToMessage()
        => new Message(Command.GET_CHANNEL_LIST, new Header());

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out GetChannelListRequest? request)
    {
        request = null;

        if (message.Command != Command.GET_CHANNEL_LIST)
        {
            throw new InvalidDataException($"Command is not get channel list: {message.Command}");
        }

        if (message.Header.Fields.Count != 0)
        {
            throw new InvalidDataException($"Extra headers found");
        }

        if (message.Payload != null)
        {
            throw new InvalidDataException("Unexpected payload");
        }

        request = new GetChannelListRequest();
        return true;
    }
}
