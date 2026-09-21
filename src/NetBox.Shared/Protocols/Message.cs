using System.Buffers.Binary;
using System.Text;

namespace NetBox.Shared.Protocols;

public class Message
{
    private const int MessageLengthByteLength = 4;
    private Header _header { get; } = new Header();
    private byte[] _payload { get; } = Array.Empty<byte>();

    /// <summary>
    /// Constructor with Header and payload.
    /// Payload for ease of use should be just strin, which gets converted to UT8.
    /// </summary>
    /// <param name="header"></param>
    /// <param name="payload"></param>
    public Message(Header header, string payload)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(payload);

        _header = header;
        _payload = Encoding.UTF8.GetBytes(payload);
    }

    public string GetMessage()
    {
        return Encoding.UTF8.GetString(_payload);
    }

    /// <summary>
    /// Serializes a message according to the following structure:
    /// [4 bytes] - Length of whole message not including these 4 bytes.
    /// [N bytes] - Header <see cref="Header"/>
    /// [4 bytes] - Payload Length
    /// [M bytes] - Payload
    /// </summary>
    /// <returns></returns>
    public byte[] Serialize()
    {
        var serializedHeader = _header.Serialize();

        byte[] payloadLength = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(payloadLength, _payload.Length);

        var totalMessageLength = serializedHeader.Length + payloadLength.Length + _payload.Length;
        byte[] messageLength = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(messageLength, totalMessageLength);

        byte[] result = new byte[MessageLengthByteLength + totalMessageLength];

        Buffer.BlockCopy(messageLength, 0, result, 0, messageLength.Length);
        Buffer.BlockCopy(serializedHeader, 0, result, messageLength.Length, serializedHeader.Length);
        Buffer.BlockCopy(payloadLength, 0, result, messageLength.Length + serializedHeader.Length, payloadLength.Length);
        Buffer.BlockCopy(_payload, 0, result, messageLength.Length + serializedHeader.Length + payloadLength.Length, _payload.Length);

        return result;
    }

    /// <summary>
    /// Deserializes message, not including the initial 4 bytes for message length.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static bool Deserialize(byte[] data, out Message? message)
    {
        try
        {
            message = null;

            if (data.Length < 4)
            {
                Console.WriteLine("Data bytes less than 4");
                return false;
            }

            var headerLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
            if (headerLength < 0)
            {
                Console.WriteLine("Invalid header length: " + headerLength);
                return false;
            }

            if (data.Length - 4 < headerLength)
            {
                Console.WriteLine("Malformed header");
                return false;
            }

            var headerBytes = data.AsSpan(4, headerLength).ToArray();

            if (!Header.Deserialize(headerBytes, out var header) || header == null)
            {
                return false;
            }

            if (data.Length - 4 - headerLength < 4)
            {
                Console.WriteLine("Malformed payload");
                return false;
            }

            var payloadSection = data.AsSpan(4 + headerBytes.Length).ToArray();
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(payloadSection.AsSpan(0, 4));
            if (payloadLength < 0)
            {
                Console.WriteLine("Invalid payload length: " + payloadLength);
                return false;
            }

            if (payloadSection.Length - 4 != payloadLength)
            {
                Console.WriteLine($"Payload lengths do not match. Expected: {payloadLength}  Actual: {payloadSection.Length - 4}");
                return false;
            }

            var payloadBytes = payloadSection.AsSpan(4, payloadLength);

            var utf8StrictEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var payload = utf8StrictEncoding.GetString(payloadBytes);

            message = new Message(header, payload);
            return true;
        }
        catch (DecoderFallbackException exception)
        {
            Console.WriteLine("Failed to decode message. Data: " + exception.BytesUnknown);
            message = null;
            return false;
        }
    }

    public void PrintString()
    {
        Console.WriteLine("Header:");
        Console.WriteLine(_header.ToString());
        Console.WriteLine("Payload:");
        Console.WriteLine(GetMessage());
    }
}
