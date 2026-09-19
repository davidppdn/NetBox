using System.Buffers.Binary;
using System.Text;
using NetBox.Shared.Protocols;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class HeaderTests
{
    [Fact]
    public void Deserialize_ValidHeader_ReturnsHeaderWithFields()
    {
        var header = new Header();
        header.AddField(new HeaderField(HeaderFieldIdEnum.Username, "alice"));
        header.AddField(new HeaderField(HeaderFieldIdEnum.Command, "RUN"));

        var bytes = header.Serialize();
        // Serialize includes a 4-byte total header length prefix; Header.Deserialize expects the payload after that prefix
        var payload = new byte[bytes.Length - 4];
        Buffer.BlockCopy(bytes, 4, payload, 0, payload.Length);

        bool ok = Header.Deserialize(payload, out var parsed);

        Assert.True(ok);
        Assert.NotNull(parsed);
        // ensure fields available and correct
        // We cannot access private list, but we can attempt to reserialize parsed and compare fields round-trip
        var reparsedBytes = parsed!.Serialize();
        // reparsed bytes include a header length prefix; compare the payload portions
        var reparsedPayload = new byte[reparsedBytes.Length - 4];
        Buffer.BlockCopy(reparsedBytes, 4, reparsedPayload, 0, reparsedPayload.Length);
        Assert.Equal(payload, reparsedPayload);
    }

    [Fact]
    public void Deserialize_EmptyOrTooShort_ReturnsFalse()
    {
        var empty = new byte[0];
        bool ok = Header.Deserialize(empty, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);

        var shortBuf = new byte[2];
        ok = Header.Deserialize(shortBuf, out parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_FieldCountMismatch_ReturnsFalse()
    {
        // Build data with fieldCount = 2 but only include one field's bytes
        var hf = new HeaderField(HeaderFieldIdEnum.Username, "bob");
        var fieldBytes = hf.Serialize();

        var buf = new byte[4 + fieldBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 2); // fieldCount=2
        Buffer.BlockCopy(fieldBytes, 0, buf, 4, fieldBytes.Length);

        bool ok = Header.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_FieldWithInvalidId_ReturnsFalse()
    {
        // Build a field with invalid id in its payload (identifier bytes set to 99) but correct length prefix
        var valueBytes = Encoding.UTF8.GetBytes("x");
        int fieldLen = 4 + valueBytes.Length;
        var field = new byte[4 + fieldLen]; // length prefix + fieldLen bytes
        BinaryPrimitives.WriteInt32BigEndian(field.AsSpan(0,4), fieldLen);
        BinaryPrimitives.WriteInt32BigEndian(field.AsSpan(4,4), 99); // invalid id
        Buffer.BlockCopy(valueBytes, 0, field, 8, valueBytes.Length);

        var buf = new byte[4 + field.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 1); // fieldCount=1
        Buffer.BlockCopy(field, 0, buf, 4, field.Length);

        bool ok = Header.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_DuplicateFieldIds_ReturnsFalse()
    {
        // Two fields with the same id should cause AddField to throw and Deserialize to fail
        var hf1 = new HeaderField(HeaderFieldIdEnum.Username, "a");
        var hf2 = new HeaderField(HeaderFieldIdEnum.Username, "b");
        var b1 = hf1.Serialize();
        var b2 = hf2.Serialize();

        var buf = new byte[4 + b1.Length + b2.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 2); // fieldCount=2
        Buffer.BlockCopy(b1, 0, buf, 4, b1.Length);
        Buffer.BlockCopy(b2, 0, buf, 4 + b1.Length, b2.Length);

        bool ok = Header.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }
}
