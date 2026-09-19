using System.Buffers.Binary;
using System.Text;
using NetBox.Shared.Protocols;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class HeaderFieldTests
{
    [Fact]
    public void Serialize_ProducesExpectedByteLayout()
    {
        var hf = new HeaderField(HeaderFieldIdEnum.Username, "alice");
        byte[] bytes = hf.Serialize();
        // Expected layout per current implementation:
        // [4 bytes] - Field Length (big-endian) = 4 (identifier) + N (value bytes)
        // [4 bytes] - Field Identifier (big-endian)
        // [N bytes] - Field Value (UTF-8 encoded)
        var valueBytes = Encoding.UTF8.GetBytes("alice");
        int fieldLength = 4 + valueBytes.Length; // identifier size + value size
        var expected = new byte[4 + 4 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(0,4), fieldLength);
        BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(4,4), (int)HeaderFieldIdEnum.Username);
        Buffer.BlockCopy(valueBytes, 0, expected, 8, valueBytes.Length);

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void Deserialize_ValidData_ReturnsHeaderField()
    {
        // use Serialize to produce valid data
        var original = new HeaderField(HeaderFieldIdEnum.Command, "RUN");
        var data = original.Serialize();
        // Deserialize expects the payload without the 4-byte length prefix
        var payload = new byte[data.Length - 4];
        Buffer.BlockCopy(data, 4, payload, 0, payload.Length);

        bool ok = HeaderField.Deserialize(payload, out var parsed);

        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(HeaderFieldIdEnum.Command, parsed!.Id);
        Assert.Equal("RUN", parsed.Value);
    }

    [Fact]
    public void Deserialize_InvalidId_ReturnsFalse()
    {
        // Build a buffer with an undefined id (e.g. 99) and no value bytes
        var buf = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 99);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }


    [Fact]
    public void Deserialize_ZeroLengthValue_Succeeds()
    {
        // For current Deserialize semantics, a zero-length value means the buffer contains only the identifier
        var buf = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldIdEnum.Username);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(string.Empty, parsed!.Value);
    }

    [Fact]
    public void Deserialize_InvalidUtf8Bytes_ReturnsFalse()
    {
        // Construct a buffer with a valid id and invalid UTF-8 bytes (0xFF bytes)
        var valueBytes = new byte[] { 0xFF, 0xFF };
        var buf = new byte[4 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldIdEnum.Command);
        Buffer.BlockCopy(valueBytes, 0, buf, 4, valueBytes.Length);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        // We expect the deserializer to detect invalid UTF-8 and fail
        Assert.False(ok);
        Assert.Null(parsed);
    }
}
