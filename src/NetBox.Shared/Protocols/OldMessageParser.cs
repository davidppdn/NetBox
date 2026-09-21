using System.Buffers.Binary;

namespace NetBox.Shared.Protocols;

public class OldMessageParser
{
    private readonly List<byte> _buffer = [];

    public OldMessageParser()
    {
    }

    public List<OldMessage> ParseBytes(byte[] data, int bytesRead)
    {
        var messages = new List<OldMessage>();
        var readData = data.AsSpan(0, bytesRead);
        _buffer.AddRange(readData);

        while (true)
        {
            var buffer = _buffer.ToArray();

            if (_buffer.Count < 4)
            {
                // Not enough data to read message length
                break;
            }

            var messageLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan());
            if (messageLength < 0)
            {
                throw new InvalidDataException("Invalid message length.");
            }

            if (_buffer.Count - 4 < messageLength)
            {
                // Not enough data for one complete message
                break;
            }

            var messageBytes = buffer.AsSpan(4, messageLength);
            if (!OldMessage.Deserialize(messageBytes.ToArray(), out var message))
            {
                throw new InvalidDataException("Failed to deserialize a message");
            }

            if (message == null)
            {
                throw new InvalidDataException("Failed to return a message");
            }

            messages.Add(message);
            _buffer.RemoveRange(0, 4 + messageLength);
        }

        return messages;
    }
}
