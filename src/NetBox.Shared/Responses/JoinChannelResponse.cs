using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Responses;

public class JoinChannelResponse : IMessage
{
    public JoinChannelResponseCode ResponseCode { get; }

    public JoinChannelResponse(JoinChannelResponseCode code)
    {
        ResponseCode = code;
    }

    public Message ToMessage()
    {
        var headerFields = new List<HeaderField>();

        byte[] responseCodeBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(responseCodeBytes, (int)ResponseCode);

        headerFields.Add(new HeaderField(HeaderFieldId.RESPONSE_CODE, responseCodeBytes));

        return new Message(Command.JOIN_CHANNEL, new Header(headerFields));
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out JoinChannelResponse? joinChannelResponse)
    {
        joinChannelResponse = null;

        if (message.Command != Command.JOIN_CHANNEL)
        {
            throw new InvalidDataException($"Command is not join channel: {message.Command}");
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

        if (!Enum.IsDefined(typeof(JoinChannelResponseCode), responseCodeInt))
        {
            throw new InvalidDataException(
                $"Invalid JoinChannelResponseCode: {responseCodeInt}");
        }

        var responseCode = (JoinChannelResponseCode)responseCodeInt;
        joinChannelResponse = new JoinChannelResponse(responseCode);
        return true;
    }
}
