using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Requests;

public class SendMessageRequest : IMessage
{
    public string Message { get; }

    public SendMessageRequest(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
    }

    public Message ToMessage()
       => new Message(Command.SEND_MESSAGE, new Header(), Message);

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out SendMessageRequest? sendMessageRequest)
    {
        sendMessageRequest = null;

        if (message.Command != Command.SEND_MESSAGE)
        {
            throw new InvalidDataException($"Command is not send message: {message.Command}");
        }

        if (message.Header.Fields.Count != 0)
        {
            throw new InvalidDataException($"Extra headers found");
        }

        var messageContent = message.Payload;
        if (messageContent == null)
        {
            throw new InvalidDataException("Missing payload");
        }

        sendMessageRequest = new SendMessageRequest(messageContent);
        return true;
    }
}
