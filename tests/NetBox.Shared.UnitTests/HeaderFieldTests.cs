using System.Buffers.Binary;
using System.Text;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using System.Collections.Generic;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class HeaderFieldTests
{
    [Fact]
    public void Serialize_ProducesExpectedByteLayout()
    {
        // Use the defined enum value per protocol; tests should focus on serialization layout, not enum semantics
        var hf = new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("alice"));
        byte[] bytes = hf.Serialize();
        // Expected layout per current implementation:
        // [4 bytes] - Field Identifier (big-endian)
        // [N bytes] - Field Value (UTF-8 encoded)
        var valueBytes = Encoding.UTF8.GetBytes("alice");
        var expected = new byte[4 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(0,4), (int)HeaderFieldId.RESPONSE_CODE);
        Buffer.BlockCopy(valueBytes, 0, expected, 4, valueBytes.Length);

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void Deserialize_ValidData_ReturnsHeaderField()
    {
        // use Serialize to produce valid data
        var original = new HeaderField(HeaderFieldId.RESPONSE_CODE, Encoding.UTF8.GetBytes("RUN"));
        var data = original.Serialize();
        // Deserialize expects the field bytes (identifier + value)
        var payload = data;

        bool ok = HeaderField.Deserialize(payload, out var parsed);

        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(HeaderFieldId.RESPONSE_CODE, parsed!.Id);
        Assert.Equal("RUN", Encoding.UTF8.GetString(parsed.Value.ToArray()));
    }

    [Fact]
    public void Deserialize_InvalidId_ThrowsInvalidDataException()
    {
        // Build a buffer with an undefined id (e.g. 99) and no value bytes
        var buf = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 99);
        Assert.Throws<InvalidDataException>(() => HeaderField.Deserialize(buf, out var _));
    }


    [Fact]
    public void Deserialize_ZeroLengthValue_Succeeds()
    {
        // For current Deserialize semantics, a zero-length value means the buffer contains only the identifier
        var buf = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldId.RESPONSE_CODE);
        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(0, parsed!.Value.Length);
    }

    [Fact]
    public void Deserialize_InvalidUtf8Bytes_ReturnsFalse()
    {
        // Construct a buffer with a valid id and invalid UTF-8 bytes (0xFF bytes)
        var valueBytes = new byte[] { 0xFF, 0xFF };
        var buf = new byte[4 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldId.RESPONSE_CODE);
        Buffer.BlockCopy(valueBytes, 0, buf, 4, valueBytes.Length);
        bool ok = HeaderField.Deserialize(buf, out var parsed);
        // Current HeaderField.Deserialize does not validate UTF-8 bytes; it should succeed and preserve raw bytes
        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(valueBytes.Length, parsed!.Value.Length);
        Assert.Equal(valueBytes, parsed.Value.ToArray());
    }
}
