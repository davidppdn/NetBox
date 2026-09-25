using NetBox.Shared.Protocols.Enums;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Protocols;

/// <summary>
/// Class: HeaderField
/// Purpose: To serialize and deserialize the following protocol structure:
/// [4 bytes] - HeaderFieldId ( int )
/// [N bytes] - HeaderField Value
/// Note: HeaderFieldId determines how the HeaderField Value gets serialized / deserialized.
/// However, we keep HeaderfieldValue as bytes and let higher level items interpret the data.
/// </summary>
public class HeaderField
{
    public HeaderFieldId Id { get; private set; }
    public byte[] Value { get; private set; }

    public HeaderField(HeaderFieldId id, byte[] value)
    {
        this.Id = id;
        this.Value = value;
    }

    /// <summary>
    /// Serializes <see cref="HeaderField"/> into the following structure:
    /// [4 bytes] - HeaderFieldId ( int )
    /// [N bytes] - HeaderField Value
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public byte[] Serialize()
    {
        var length = 4 + Value.Length;
        var bytes = new byte[length];

        BinaryPrimitives.WriteInt32BigEndian(bytes, (int)Id);
        Value.CopyTo(bytes, 4);

        return bytes;
    }

    /// <summary>
    /// Deserializes a readonlyspan of bytes into <see cref="HeaderField"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="headerField"></param>
    /// <returns>true if deserialization is successful, false if not enough data, throws an excpetion if malformed</returns>
    /// <exception cref="InvalidDataException">
    /// This exceptions is thrown when the 4 bytes representing the id de-serialize into a non-defined id.
    /// </exception>
    public static bool Deserialize(
       ReadOnlySpan<byte> data,
       [NotNullWhen(true)] out HeaderField? headerField)
    {
        headerField = null;
        int pt = 0;
        int length = 4;

        if (data.Length < pt + length)
        {
            return false;
        }

        var headerFieldIdInt = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
        if (!Enum.IsDefined(typeof(HeaderFieldId), headerFieldIdInt))
        {
            throw new InvalidDataException($"There is not corresponding command for commandId: {headerFieldIdInt}");
        }

        var headerFieldId = (HeaderFieldId)headerFieldIdInt;

        pt += length;
        length = data.Length - pt;

        var value = data.Slice(pt).ToArray();

        headerField = new HeaderField(headerFieldId, value);
        return true;
    }
}
