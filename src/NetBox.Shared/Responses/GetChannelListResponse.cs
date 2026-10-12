using NetBox.Shared.Interfaces;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBox.Shared.Responses;

public class GetChannelListResponse : IMessage
{
    public GetChannelListResponseCode ResponseCode { get; }
    public IReadOnlyList<string>? Channels { get; }

    public GetChannelListResponse(GetChannelListResponseCode code, IReadOnlyList<string>? channels = null)
    {
        ResponseCode = code;
        Channels = channels;
    }

    public Message ToMessage()
    {
        var headerFields = new List<HeaderField>();

        byte[] responseCodeBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(responseCodeBytes, (int)ResponseCode);
        headerFields.Add(new HeaderField(HeaderFieldId.RESPONSE_CODE, responseCodeBytes));

        // Build payload bytes
        byte[]? payload = null;
        if (Channels != null)
        {
            var writer = new ArrayBufferWriter<byte>();
            var countBytes = writer.GetSpan(4);
            BinaryPrimitives.WriteInt32BigEndian(countBytes, Channels.Count);
            writer.Advance(4);

            foreach (var ch in Channels)
            {
                var nameBytes = Encoding.UTF8.GetBytes(ch);
                var lenSpan = writer.GetSpan(4);
                BinaryPrimitives.WriteInt32BigEndian(lenSpan, nameBytes.Length);
                writer.Advance(4);

                var nameSpan = writer.GetSpan(nameBytes.Length);
                nameBytes.CopyTo(nameSpan);
                writer.Advance(nameBytes.Length);
            }

            payload = writer.WrittenMemory.ToArray();
        }

        var header = new Header(headerFields);
        return new Message(Command.GET_CHANNEL_LIST, header, payload);
    }

    public static bool FromMessage(
        Message message,
        [NotNullWhen(true)] out GetChannelListResponse? response)
    {
        response = null;

        if (message.Command != Command.GET_CHANNEL_LIST)
        {
            throw new InvalidDataException($"Command is not get channel list: {message.Command}");
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
            throw new InvalidDataException($"Invalid response code length: {value.Length}");
        }

        var responseCodeInt = BinaryPrimitives.ReadInt32BigEndian(value);
        if (!Enum.IsDefined(typeof(GetChannelListResponseCode), responseCodeInt))
        {
            throw new InvalidDataException($"Invalid GetChannelListResponseCode: {responseCodeInt}");
        }

        var responseCode = (GetChannelListResponseCode)responseCodeInt;

        // Enforce response-code / payload relationship per protocol
        if (responseCode == GetChannelListResponseCode.SUCCESS)
        {
            // Success must include a payload (even if empty list -> 4 bytes zero count)
            if (message.Payload == null)
            {
                throw new InvalidDataException("SUCCESS response missing payload");
            }

            var payloadBytes = message.Payload;
            int pt = 0;

            // Need at least 4 bytes for the count
            if (payloadBytes.Length < 4)
            {
                throw new InvalidDataException("Invalid payload for get channel list");
            }

            int count = BinaryPrimitives.ReadInt32BigEndian(payloadBytes.AsSpan(pt, 4));
            pt += 4;

            if (count < 0)
            {
                throw new InvalidDataException("Negative channel count");
            }

            // Prevent obvious overallocation by ensuring there is at least 4 bytes per entry for the length field
            if (count > (payloadBytes.Length - pt) / 4)
            {
                throw new InvalidDataException("Channel count exceeds available payload data");
            }

            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var channelsList = new List<string>(Math.Max(0, Math.Min(count, 1024)));

            for (int i = 0; i < count; i++)
            {
                if (payloadBytes.Length - pt < 4) throw new InvalidDataException("Invalid payload while reading name length");
                int nameLen = BinaryPrimitives.ReadInt32BigEndian(payloadBytes.AsSpan(pt, 4));
                pt += 4;

                if (nameLen < 0) throw new InvalidDataException("Negative channel name length");
                if (nameLen > payloadBytes.Length - pt) throw new InvalidDataException("Invalid payload while reading name bytes");

                var name = utf8.GetString(payloadBytes, pt, nameLen);
                pt += nameLen;
                channelsList.Add(name);
            }

            if (pt != payloadBytes.Length) throw new InvalidDataException("Unexpected trailing payload bytes");

            response = new GetChannelListResponse(responseCode, channelsList);
        }
        else
        {
            // FAILURE must not include a payload
            if (message.Payload != null)
            {
                throw new InvalidDataException("FAILURE response must not contain a payload");
            }

            response = new GetChannelListResponse(responseCode, null);
        }
        return true;
    }
}
