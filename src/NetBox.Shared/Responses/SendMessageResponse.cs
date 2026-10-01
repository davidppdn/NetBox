using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Responses;

public class SendMessageResponse : IMessage
{
    public SendMessageResponseCode ResponseCode { get; }

    public SendMessageResponse(SendMessageResponseCode code)
    {
        ResponseCode = code;
    }

    public Message ToMessage()
    {
        var headerFields = new List<HeaderField>();

        byte[] responseCodeBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(responseCodeBytes, (int)ResponseCode);

        headerFields.Add(new HeaderField(HeaderFieldId.RESPONSE_CODE, responseCodeBytes));

        return new Message(Command.SEND_MESSAGE, new Header(headerFields));
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out SendMessageResponse? sendMessageResponse)
    {
        sendMessageResponse = null;

        if (message.Command != Command.SEND_MESSAGE)
        {
            throw new InvalidDataException($"Command is not send message: {message.Command}");
        }

        if (message.Header.Fields.Count != 1)
        {
            throw new InvalidDataException($"Incorrect number of headers, expecting one.");
        }

        var headerField = message.Header.Fields[0];

        if (headerField.Id != HeaderFieldId.RESPONSE_CODE)
        {
            throw new InvalidDataException($"Incorrect headerfield id.");
        }

        var value = headerField.Value;

        if (value.Length != 4)
        {
            throw new InvalidDataException(
                $"Invalid response code length: {value.Length}");
        }

        var responseCodeInt = BinaryPrimitives.ReadInt32BigEndian(value);

        if (!Enum.IsDefined(typeof(SendMessageResponseCode), responseCodeInt))
        {
            throw new InvalidDataException(
                $"Invalid SendMessageResponseCode: {responseCodeInt}");
        }

        var responseCode = (SendMessageResponseCode)responseCodeInt;
        sendMessageResponse = new SendMessageResponse(responseCode);
        return true;
    }
}
