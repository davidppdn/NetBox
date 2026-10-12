using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBox.Shared.Requests;

public class CreateChannelRequest : IMessage
{
    public string ChannelName { get; }

    public CreateChannelRequest(string channelName)
    {
        ArgumentNullException.ThrowIfNull(channelName);
        ChannelName = channelName;
    }
    
    public Message ToMessage()
        => new Message(Command.CREATE_CHANNEL, new Header(), System.Text.Encoding.UTF8.GetBytes(ChannelName));

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out CreateChannelRequest? createChannelRequest)
    {
        createChannelRequest = null;

        if (message.Command != Command.CREATE_CHANNEL)
        {
            throw new InvalidDataException($"Command is not create channel: {message.Command}");
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

        createChannelRequest = new CreateChannelRequest(channelName);
        return true;

    }
}