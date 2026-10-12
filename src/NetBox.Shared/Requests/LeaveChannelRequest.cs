using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Requests;

public class LeaveChannelRequest : IMessage
{
    public LeaveChannelRequest()
    {
    }

    public Message ToMessage()
    {
        return new Message(Command.LEAVE_CHANNEL, new Header());
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out LeaveChannelRequest? request)
    {
        request = null;

        if (message.Command != Command.LEAVE_CHANNEL)
        {
            throw new InvalidDataException($"Command is not leave channel: {message.Command}");
        }

        if (message.Header.Fields.Count != 0)
        {
            throw new InvalidDataException($"Extra headers found");
        }

        if (message.Payload != null)
        {
            throw new InvalidDataException("Unexpected payload");
        }

        request = new LeaveChannelRequest();
        return true;
    }
}
