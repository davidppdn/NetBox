using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.ServerEvents;

public class ChatMessage : IMessage
{
    public string Sender { get; }
    public string Message { get; }
    public ChatMessage(string sender, string message)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(message);
        Sender = sender;
        Message = message;
    }

    public Message ToMessage()
    {
        var headerField = new HeaderField(HeaderFieldId.SENDER, System.Text.Encoding.UTF8.GetBytes(Sender));
        var header = new Header(new List<HeaderField> { headerField });
        return new Message(Command.CHAT_MESSAGE, header, Message);
    }

    public static bool FromMessage(
        Message message, 
        [NotNullWhen(true)]out ChatMessage? chatMessage)
    {
        chatMessage = null;

        if (message.Command != Command.CHAT_MESSAGE)
        {
            return false;
        }
        
        var headerFields = message.Header.Fields;

        if (headerFields.Count != 1)
        {
            throw new InvalidDataException($"Incorrect header fields for command: {message.Command}");
        }

        var senderField = headerFields[0];

        if (senderField.Id != HeaderFieldId.SENDER)
        {
            throw new InvalidDataException($"Incorrect header field id for command: {message.Command}");
        }

        var sender = System.Text.Encoding.UTF8.GetString(senderField.Value);

        if (message.Payload == null)
        {
            throw new InvalidDataException($"Payload is null for command: {message.Command}");
        }

        chatMessage = new ChatMessage(sender, message.Payload ?? string.Empty);
        return true;
    }
}
