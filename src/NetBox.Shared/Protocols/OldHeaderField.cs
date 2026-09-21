using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;

namespace NetBox.Shared.Protocols;

public class OldHeaderField
{
    private const int fieldIdentifierByteSize = 4;
    private const int fieldLengthByteSize = 4;

    public readonly OldHeaderFieldIdEnum Id;
    public readonly string Value;

    public OldHeaderField(OldHeaderFieldIdEnum id, string value)
    {
        Id = id;
        Value = value;
    }

    /// <summary>
    /// Converts the HeaderField instance into a byte array representation.
    /// Used for protocol serialization. The format is as follows:
    /// [4 bytes] - Field Length (big-endian)
    /// [4 bytes] - Field Identifier (big-endian)
    /// [N bytes] - Field Value (UTF-8 encoded)
    /// </summary>
    /// <returns></returns>
    public byte[] Serialize()
    {
        byte[] fieldIdentifier = new byte[fieldIdentifierByteSize];
        BinaryPrimitives.WriteInt32BigEndian(fieldIdentifier, (int)Id);

        byte[] valueBytes = Encoding.UTF8.GetBytes(Value);

        byte[] fieldLength = new byte[fieldLengthByteSize];
        BinaryPrimitives.WriteInt32BigEndian(fieldLength, valueBytes.Length + fieldIdentifier.Length);

        byte[] result = new byte[fieldLength.Length + fieldIdentifier.Length + valueBytes.Length];
        
        Buffer.BlockCopy(fieldLength, 0, result, 0, fieldLength.Length);
        Buffer.BlockCopy(fieldIdentifier, 0, result, fieldLength.Length, fieldIdentifier.Length);
        Buffer.BlockCopy(valueBytes, 0, result, fieldLength.Length + fieldIdentifier.Length, valueBytes.Length);

        return result;
    }

    /// <summary>
    /// Attempts to deserialize a byte array back into a HeaderField instance.
    /// Note:
    /// The data should only be the [identifier][data], the length prefix is not included in the data array.
    /// </summary>
    /// <param name="data">An array of bytes representing a single serialized HeaderField.</param>
    /// <param name="headerField">The deserialized HeaderField instance, or null if deserialization fails.</param>
    /// <returns>true if deserialization is successful; otherwise, false.</returns>
    public static bool Deserialize(byte[] data, out OldHeaderField? headerField)
    {
        try
        {
            if (data.Length < fieldIdentifierByteSize)
            {
                headerField = null;
                return false;
            }

            int fieldIdentifier = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, fieldIdentifierByteSize));
            if (!Enum.IsDefined(typeof(OldHeaderFieldIdEnum), fieldIdentifier))
            {
                headerField = null;
                return false;
            }

            var utf8StrictEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            string value = utf8StrictEncoding.GetString(data, fieldIdentifierByteSize, data.Length - fieldIdentifierByteSize);
            headerField = new OldHeaderField((OldHeaderFieldIdEnum)fieldIdentifier, value);
            return true;
        }
        catch (DecoderFallbackException exception)
        {
            Console.WriteLine("Failed to decode message. Data: " + exception.BytesUnknown);
            headerField = null;
            return false;
        }
    }
}
