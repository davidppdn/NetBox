using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Responses;

public class LeaveChannelResponse : IMessage
{
    public LeaveChannelResponseCode ResponseCode { get; }

    public LeaveChannelResponse(LeaveChannelResponseCode code)
    {
        ResponseCode = code;
    }

    public Message ToMessage()
    {
        var headerFields = new List<HeaderField>();

        byte[] responseCodeBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(responseCodeBytes, (int)ResponseCode);

        headerFields.Add(new HeaderField(HeaderFieldId.RESPONSE_CODE, responseCodeBytes));

        return new Message(Command.LEAVE_CHANNEL, new Header(headerFields));
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out LeaveChannelResponse? leaveChannelResponse)
    {
        leaveChannelResponse = null;

        if (message.Command != Command.LEAVE_CHANNEL)
        {
            throw new InvalidDataException($"Command is not leave channel: {message.Command}");
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

        if (!Enum.IsDefined(typeof(LeaveChannelResponseCode), responseCodeInt))
        {
            throw new InvalidDataException(
                $"Invalid LeaveChannelResponseCode: {responseCodeInt}");
        }

        var responseCode = (LeaveChannelResponseCode)responseCodeInt;
        leaveChannelResponse = new LeaveChannelResponse(responseCode);
        return true;
    }
}
