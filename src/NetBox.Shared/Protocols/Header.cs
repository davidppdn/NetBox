using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Shared.Protocols;

/// <summary>
/// Class: Header
/// Purpose: To serialize and deserialize the following protocol structure:
/// Header contains the following:
/// [4 bytes] - HeaderField Count
/// [X bytes] - HeaderFields
/// HeaderField contains the following:
/// [4 bytes] - HeaderField Length ( bytes )
/// [Y bytes] - HeaderField
/// </summary>
public class Header
{
    public List<HeaderField> Fields { get; private set; }

    public Header(List<HeaderField>? fields = null)
    {
        this.Fields = fields ?? [];
    }

    /// <summary>
    /// Serializes the Header into the following structure
    /// [4 bytes] - HeaderField Count
    /// [X bytes] - HeaderFields
    /// HeaderField contains the following:
    /// [4 bytes] - HeaderField Length ( bytes )
    /// [Y bytes] - HeaderField
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public byte[] Serialize()
    {
        var writer = new ArrayBufferWriter<byte>();

        var countSpan = writer.GetSpan(4);
        BinaryPrimitives.WriteInt32BigEndian(countSpan, Fields.Count);
        writer.Advance(4);

        foreach (var field in Fields)
        {
            var fieldBytes = field.Serialize();

            var lengthSpan = writer.GetSpan(4);
            BinaryPrimitives.WriteInt32BigEndian(lengthSpan, fieldBytes.Length);
            writer.Advance(4);

            var fieldSpan = writer.GetSpan(fieldBytes.Length);
            fieldBytes.CopyTo(fieldSpan);
            writer.Advance(fieldBytes.Length);
        }

        return writer.WrittenMemory.ToArray();
    }

    /// <summary>
    /// Deserializes readonlyspan of bytes into the header object. The following structure is expected:
    /// [4 bytes] - HeaderField Count ( mandatory, but can be 0 )
    /// [4 bytes] - HeaderField Length ( bytes )
    /// [Y bytes] - HeaderField
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool Deserialize(
        ReadOnlySpan<byte> data,
        [NotNullWhen(true)] out Header? header)
    {
        header = null;

        int pt = 0;
        int length = 4;

        if (data.Length < pt + length)
        {
            return false;
        }

        var headerCount = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
        if (headerCount < 0)
        {
            throw new InvalidDataException($"Header Count cannot be less than 0: {headerCount}");
        }

        if (headerCount == 0 && data.Length > 4)
        {
            // Considers the situation where there should be no header fields, but there is excess data
            throw new InvalidDataException($"Header count is 0, but there are {data.Length - 4} extra bytes.");
        }

        if (headerCount == 0)
        {
            header = new Header();
            return true;
        }

        pt += length;
        length = 4;

        var headerFields = new List<HeaderField>();

        for (var i = 0; i < headerCount; i++)
        {
            if (data.Length < pt + length)
            {
                return false;
            }

            var headerFieldLength = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
            if (headerFieldLength < 0)
            {
                throw new InvalidDataException($"Header Field Length cannot be less than 0: {headerFieldLength}");
            }

            pt += length;
            length = headerFieldLength;

            if (data.Length < pt + length)
            {
                return false;
            }

            var headerFieldBytes = data.Slice(pt, length);
            if (!HeaderField.Deserialize(headerFieldBytes, out var headerField))
            {
                header = null;
                return false;
            }

            headerFields.Add(headerField);

            pt += length;
            length = 4;
        }

        if (data.Length > pt)
        {
            throw new InvalidDataException(
                $"Header contains {data.Length - pt} unexpected trailing bytes.");
        }

        header = new Header(headerFields);
        return true;
    }
}
