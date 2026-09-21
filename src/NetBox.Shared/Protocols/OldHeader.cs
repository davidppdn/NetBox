using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols;

public class OldHeader
{
    private const int fieldCountByteSize = 4;
    private const int headerLengthByteSize = 4;

    private List<OldHeaderField> _fields = new List<OldHeaderField>();

    public OldHeader()
    {
    }

    public void AddField(OldHeaderField field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        if (_fields.Exists(f => f.Id == field.Id))
                throw new ArgumentException($"A field with the same Id ({field.Id}) already exists in the header.");

        _fields.Add(field);
    }

    /// <summary>
    /// Converts the Header instance into a byte array representation.
    /// Follows the following format:
    /// [4 bytes] - Header Length (big-endian)
    /// [4 bytes] - Field Count (big-endian)
    /// [N bytes] - Field Data
    /// </summary>
    /// <returns></returns>
    public byte[] Serialize()
    {
        var fieldCount = _fields.Count;
        byte[] fieldCountBytes = new byte[fieldCountByteSize];
        BinaryPrimitives.WriteInt32BigEndian(fieldCountBytes, fieldCount);

        var finalBytes = new List<byte>();

        foreach (var field in _fields)
        {
            var fieldBytes = field.Serialize();
            finalBytes.AddRange(fieldBytes);
        }

        finalBytes.InsertRange(0, fieldCountBytes);

        var headerLength = finalBytes.Count;
        byte[] headerLengthBytes = new byte[headerLengthByteSize];
        BinaryPrimitives.WriteInt32BigEndian(headerLengthBytes, headerLength);

        finalBytes.InsertRange(0, headerLengthBytes);

        return finalBytes.ToArray();
    }

    /// <summary>
    /// Deserializes a byte array into a Header instance.
    /// Deserialization only includes:
    /// Header Field Count
    /// Header Fields
    /// The header length is not deserialized as it is not needed for the Header instance,
    /// and is consumed by the Protocol class to determine how many bytes to read for the header.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="header"></param>
    /// <returns></returns>
    public static bool Deserialize(byte[] data, out OldHeader? header)
    {
        header = null;

        if (data.Length < fieldCountByteSize)
            return false;

        var fieldCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, fieldCountByteSize));

        if (fieldCount < 0)
            return false;

        header = new OldHeader();
        if (fieldCount == 0)
            return true;

        var remainingData = data.AsSpan(fieldCountByteSize);
        var startIndex = 0;

        try
        {
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldLengthByteEndIndex = startIndex + 4;

                var fieldLengthBytes = remainingData.Slice(startIndex, 4);

                var fieldLength = BinaryPrimitives.ReadInt32BigEndian(fieldLengthBytes);

                if (fieldLength < 0)
                {
                    header = null;
                    return false;
                }

                var headerFieldBytes = remainingData.Slice(fieldLengthByteEndIndex, fieldLength).ToArray();

                if (OldHeaderField.Deserialize(headerFieldBytes, out var headerField))
                {
                    if (headerField == null)
                    {
                        Console.WriteLine("Header deserialize returned null: " + headerFieldBytes.ToString());
                        header = null;
                        return false;
                    }

                    header.AddField(headerField);
                }
                else
                {
                    header = null;
                    return false;
                }

                startIndex += 4 + fieldLength;
            }
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Console.WriteLine("An error occured parsing header:" + exception.Message);
            header = null;
            return false;
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occured parsing headers:" + exception.Message);
            header = null;
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        string result = "";
        foreach (var headerField in _fields)
        {
            result += $"Id: {headerField.Id} Value: {headerField.Value}\n";
        }
        return result;
    }
}
