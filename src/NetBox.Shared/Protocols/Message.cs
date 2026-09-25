using NetBox.Shared.Protocols.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NetBox.Shared.Protocols;

/// <summary>
/// Class: Message
/// Purpose: To serialize and deserialize the following protocol structure:
/// [4 bytes] - Command Id      Integer
/// [4 bytes] - Header Length   ( count in bytes )
/// [N bytes] - Header
/// [4 bytes] - Payload Length  ( count in bytes )
/// [M bytes] - Payload         ( strictly UTF-8 ) ( optional )
/// </summary>
public class Message
{
    private Command _command;
    private Header _header;
    private string? _payload;

    public Message(Command command, Header header, string? payload = null)
    {
        _command = command;
        _header = header;
        _payload = payload;
    }

    /// <summary>
    /// Serialize the message into the following structure:
    /// [4 bytes] - Message length ( not including these 4 bytes]
    /// [4 bytes] - Command Id
    /// [4 bytes] - Header Length   ( count in bytes )
    /// [N bytes] - Header
    /// [4 bytes] - Payload Length  ( count in bytes )
    /// [M bytes] - Payload         ( strictly UTF-8 ) ( optional )
    /// </summary>
    /// <returns></returns>
    public byte[] Serialize()
    {
        var headerSerialized = _header.Serialize();
        var payloadSerialized = _payload != null ? Encoding.UTF8.GetBytes(_payload) : null;

        var messageLengthBytes = 4;
        var commandIdLengthBytes = 4;
        var headerLengthBytes = 4;
        var headerBytes = headerSerialized.Length;
        var payloadLengthBytes = 4;
        var payloadLength = payloadSerialized != null ? payloadSerialized.Length : 0;

        /// messagelengthbytes not included in message length
        var messageLength = commandIdLengthBytes
                            + headerLengthBytes
                            + headerBytes
                            + payloadLengthBytes
                            + payloadLength;

        var writer = new ArrayBufferWriter<byte>();

        var messageLengthSpan = writer.GetSpan(messageLengthBytes);
        BinaryPrimitives.WriteInt32BigEndian(messageLengthSpan, messageLength);
        writer.Advance(messageLengthBytes);

        var commandIdSpan = writer.GetSpan(commandIdLengthBytes);
        BinaryPrimitives.WriteInt32BigEndian(commandIdSpan, (int)_command);
        writer.Advance(commandIdLengthBytes);

        var headerLengthSpan = writer.GetSpan(headerLengthBytes);
        BinaryPrimitives.WriteInt32BigEndian(headerLengthSpan, headerSerialized.Length);
        writer.Advance(headerLengthBytes);

        var headerSpan = writer.GetSpan(headerBytes);
        headerSerialized.CopyTo(headerSpan);
        writer.Advance(headerBytes);

        var payloadLengthSpan = writer.GetSpan(payloadLengthBytes);
        BinaryPrimitives.WriteInt32BigEndian(payloadLengthSpan, payloadLength);
        writer.Advance(payloadLengthBytes);

        if (payloadLength > 0 && payloadSerialized != null)
        {
            var payloadSpan = writer.GetSpan(payloadLength);
            payloadSerialized.CopyTo(payloadSpan);
            writer.Advance(payloadLength);
        }

        return writer.WrittenMemory.ToArray();
    }


    /// <summary>
    /// Deserialize a span of bytes into a single message according to the following shape:
    /// [4 bytes] - Command Id      Integer
    /// [4 bytes] - Header Length   ( count in bytes )
    /// [N bytes] - Header
    /// [4 bytes] - Payload Length  ( count in bytes )
    /// [M bytes] - Payload         ( strictly UTF-8 string ) ( optional )
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <returns>
    /// Returns false if the data is correct, but incomplete.
    /// Returns true if the data is complete and correct.
    /// Throws error if data is incorrect.
    /// </returns>
    public static bool Deserialize(
        ReadOnlySpan<byte> data, 
        [NotNullWhen(true)]out Message? message)
    {
        message = null;

        try 
        {
            // Command Id
            int pt = 0;
            int length = 4;
            if (data.Length < pt + length)
            {
                return false;
            }

            var commandId = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
            if (!Enum.IsDefined(typeof(Command), commandId))
            {
                throw new InvalidDataException($"There is not corresponding command for commandId: {commandId}");
            }

            var command = (Command)commandId;

            // Header Length
            pt += length;
            length = 4;
            if (data.Length < pt + length)
            {
                return false;
            }

            var headerLength = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
            if (headerLength < 0)
            {
                throw new InvalidDataException($"Invalid header length value: {headerLength}");
            }

            // Header
            pt += length;
            length = headerLength;
            if (data.Length < pt + length)
            {
                return false;
            }

            var headerData = data.Slice(pt, length);
            if (!Header.Deserialize(headerData, out var header))
            {
                return false;
            }

            // Payload Length
            pt += length;
            length = 4;
            if (data.Length < pt + length)
            {
                return false;
            }

            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(data.Slice(pt, length));
            if (payloadLength < 0)
            {
                throw new InvalidDataException($"Invalid payload length value: {payloadLength}");
            }

            if (payloadLength == 0)
            {
                pt += length;

                if (data.Length != pt)
                {
                    throw new InvalidDataException(
                        $"Unexpected trailing data: {data.Length - pt} bytes.");
                }

                message = new Message(command, header);
                return true;
            }

            // Payload
            pt += length;
            length = payloadLength;
            if (data.Length < pt + length)
            {
                return false;
            }

            if (data.Length > pt + length)
            {
                throw new InvalidDataException($"Payload Length and length of data do not match. Expected: {payloadLength} Actual: {data.Length - pt}");
            }

            var payloadBytes = data.Slice(pt, length);
            var utf8StrictEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var payload = utf8StrictEncoding.GetString(payloadBytes);

            message = new Message(command, header, payload);
            return true;
        }
        catch (DecoderFallbackException exception)
        {
            Console.WriteLine($"Failed to decode the payload into UTF8: {exception.BytesUnknown}");
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Failed to deserialize the message: {exception.Message}");
            throw;
        }
    }
}
