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

        // Expected layout: [4 bytes id BE][4 bytes length BE][N bytes UTF8 value]
        var valueBytes = Encoding.UTF8.GetBytes("alice");
        var expected = new byte[4 + 4 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(0,4), (int)HeaderFieldIdEnum.Username);
        BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(4,4), valueBytes.Length);
        Buffer.BlockCopy(valueBytes, 0, expected, 8, valueBytes.Length);

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void Deserialize_ValidData_ReturnsHeaderField()
    {
        // use Serialize to produce valid data
        var original = new HeaderField(HeaderFieldIdEnum.Command, "RUN");
        var data = original.Serialize();

        bool ok = HeaderField.Deserialize(data, out var parsed);

        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(HeaderFieldIdEnum.Command, parsed!.Id);
        Assert.Equal("RUN", parsed.Value);
    }

    [Fact]
    public void Deserialize_TooShortData_ReturnsFalse()
    {
        var tooShort = new byte[6]; // less than 8 required bytes
        bool ok = HeaderField.Deserialize(tooShort, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_InvalidId_ReturnsFalse()
    {
        // Build a buffer with an undefined id (e.g. 99) and zero length
        var buf = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 99);
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4,4), 0);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_LengthMismatch_ReturnsFalse()
    {
        // Declared length 5 but provide only 3 bytes of value -> mismatch
        var valueBytes = Encoding.UTF8.GetBytes("abc");
        var buf = new byte[8 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldIdEnum.ResponseCode);
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4,4), 5); // declared length 5
        Buffer.BlockCopy(valueBytes, 0, buf, 8, valueBytes.Length); // only 3 bytes present

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.False(ok);
        Assert.Null(parsed);
    }

    [Fact]
    public void Deserialize_ZeroLengthValue_Succeeds()
    {
        var buf = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldIdEnum.Username);
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4,4), 0);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        Assert.True(ok);
        Assert.NotNull(parsed);
        Assert.Equal(string.Empty, parsed!.Value);
    }

    [Fact]
    public void Deserialize_InvalidUtf8Bytes_ReturnsFalse()
    {
        // Construct a buffer with a valid id and declared length but invalid UTF-8 bytes (0xFF is invalid)
        var valueBytes = new byte[] { 0xFF, 0xFF };
        var buf = new byte[8 + valueBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), (int)HeaderFieldIdEnum.Command);
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4,4), valueBytes.Length);
        Buffer.BlockCopy(valueBytes, 0, buf, 8, valueBytes.Length);

        bool ok = HeaderField.Deserialize(buf, out var parsed);
        // We expect the deserializer to detect invalid UTF-8 and fail
        Assert.False(ok);
        Assert.Null(parsed);
    }
}
