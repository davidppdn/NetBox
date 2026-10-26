using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBox.Shared.ServerEvents;

public class LeaveMessage : IMessage
{
    public string Sender { get; }

    public LeaveMessage(string sender)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sender);
        Sender = sender;
    }

    public Message ToMessage()
    {
        var headerField = new HeaderField(HeaderFieldId.SENDER, System.Text.Encoding.UTF8.GetBytes(Sender));
        var header = new Header(new List<HeaderField> { headerField });
        return new Message(Command.LEAVE_MESSAGE, header);
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out LeaveMessage? leaveMessage)
    {
        leaveMessage = null;

        if (message.Command != Command.LEAVE_MESSAGE)
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

        if (message.Payload != null)
        {
            throw new InvalidDataException($"Payload is not null for command: {message.Command}");
        }

        leaveMessage = new LeaveMessage(sender);
        return true;
    }
}
