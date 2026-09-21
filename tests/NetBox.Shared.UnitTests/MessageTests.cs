using System.Buffers.Binary;
using System.Text;
using NetBox.Shared.Protocols;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class MessageTests
{
    [Fact]
    public void SerializeDeserialize_RoundTrip()
    {
        var header = new OldHeader();
        header.AddField(new OldHeaderField(OldHeaderFieldIdEnum.Username, "alice"));

        var msg = new OldMessage(header, "payload");
        var bytes = msg.Serialize();

        // Message.Deserialize expects the data starting at the header length (skip the initial 4-byte message length)
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        bool ok = OldMessage.Deserialize(payload, out var parsed);
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
        bool ok = OldMessage.Deserialize(empty, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_MalformedHeaderLength_ReturnsFalse()
    {
        var header = new OldHeader();
        header.AddField(new OldHeaderField(OldHeaderFieldIdEnum.Username, "bob"));
        var msg = new OldMessage(header, "data");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        // Tamper header length to be larger than available
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0,4), payload.Length + 100);

        bool ok = OldMessage.Deserialize(payload, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_PayloadLengthMismatch_ReturnsFalse()
    {
        var header = new OldHeader();
        header.AddField(new OldHeaderField(OldHeaderFieldIdEnum.Username, "bob"));
        var msg = new OldMessage(header, "hello");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        int headerLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0,4));
        int payloadSectionIndex = 4 + headerLen;

        // read existing payload length
        int existingPayloadLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(payloadSectionIndex,4));
        // write a wrong payload length
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(payloadSectionIndex,4), existingPayloadLen + 1);

        bool ok = OldMessage.Deserialize(payload, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_InvalidUtf8Payload_ReturnsFalse()
    {
        var header = new OldHeader();
        header.AddField(new OldHeaderField(OldHeaderFieldIdEnum.Username, "bob"));
        var msg = new OldMessage(header, "ok");
        var bytes = msg.Serialize();
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        int headerLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0,4));
        int payloadSectionIndex = 4 + headerLen;
        int payloadLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(payloadSectionIndex,4));

        // Replace payload bytes with invalid UTF-8 sequences (same length)
        for (int i = 0; i < payloadLen; i++)
            payload[payloadSectionIndex + 4 + i] = 0xFF;

        bool ok = OldMessage.Deserialize(payload, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }
}
