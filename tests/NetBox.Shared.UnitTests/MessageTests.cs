using System.Buffers.Binary;
using System.Text;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class MessageTests
{
    [Fact]
    public void SerializeDeserialize_RoundTrip()
    {
        var header = new Header(new List<HeaderField> { new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("alice")) });

        var msg = new Message(Command.LOGIN, header, "payload");
        var bytes = msg.Serialize();

        // Message.Deserialize expects the data starting at the header length (skip the initial 4-byte message length)
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        bool ok = Message.Deserialize(payload, out var parsed);
        Assert.True(ok);
        Assert.NotNull(parsed);

        // Re-serialize the parsed message and compare to original full bytes
        var reparsed = parsed!.Serialize();
        Assert.Equal(bytes, reparsed);
    }

    [Fact]
    public void Deserialize_EmptyData_ReturnsFalse()
    {
        var empty = Array.Empty<byte>();
        bool ok = Message.Deserialize(empty, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_MalformedHeaderLength_ReturnsFalse()
    {
        var header = new Header(new List<HeaderField> { new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("bob")) });
        var msg = new Message(Command.LOGIN, header, "data");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        // Tamper header length to be larger than available
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0,4), payload.Length + 100);

        Assert.Throws<InvalidDataException>(() => Message.Deserialize(payload, out var _));
    }

    [Fact]
    public void Deserialize_PayloadLengthMismatch_ReturnsFalse()
    {
        var header = new Header(new List<HeaderField> { new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("bob")) });
        var msg = new Message(Command.LOGIN, header, "hello");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        int headerLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0,4));
        int payloadSectionIndex = 4 + headerLen;

        // read existing payload length
        int existingPayloadLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(payloadSectionIndex,4));
        // write a wrong payload length
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(payloadSectionIndex,4), existingPayloadLen + 1);

        Assert.Throws<InvalidDataException>(() => Message.Deserialize(payload, out var _));
    }

    [Fact]
    public void Deserialize_InvalidUtf8Payload_ReturnsFalse()
    {
        var header = new Header(new List<HeaderField> { new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("bob")) });
        var msg = new Message(Command.LOGIN, header, "ok");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        int headerLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0,4));
        int payloadSectionIndex = 4 + headerLen;
        int payloadLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(payloadSectionIndex,4));

        // Replace payload bytes with invalid UTF-8 sequences (same length)
        for (int i = 0; i < payloadLen; i++)
            payload[payloadSectionIndex + 4 + i] = 0xFF;

        Assert.Throws<InvalidDataException>(() => Message.Deserialize(payload, out var _));
    }
}
