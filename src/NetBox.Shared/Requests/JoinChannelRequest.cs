using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBox.Shared.Requests;

public class JoinChannelRequest : IMessage
{
    public string ChannelName { get; }

    public JoinChannelRequest(string channelName)
    {
        ArgumentNullException.ThrowIfNull(channelName);
        ChannelName = channelName;
    }

    public Message ToMessage()
        => new Message(Command.JOIN_CHANNEL, new Header(), System.Text.Encoding.UTF8.GetBytes(ChannelName));

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out JoinChannelRequest? request)
    {
        request = null;

        if (message.Command != Command.JOIN_CHANNEL)
        {
            throw new InvalidDataException($"Command is not join channel: {message.Command}");
        }

        if (message.Header.Fields.Count != 0)
        {
            throw new InvalidDataException($"Extra headers found");
        }

        var payload = message.Payload;
        if (payload == null)
        {
            throw new InvalidDataException("Missing payload");
        }

        if (payload.Length == 0)
        {
            throw new InvalidDataException("Empty payload");
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var channelName = utf8.GetString(payload);

        request = new JoinChannelRequest(channelName);
        return true;
    }
}
