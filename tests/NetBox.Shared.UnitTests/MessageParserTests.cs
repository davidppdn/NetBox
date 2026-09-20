using System.Buffers.Binary;
using NetBox.Shared.Protocols;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class MessageParserTests
{
    [Fact]
    public void ParseBytes_SingleCompleteMessage_ReturnsMessage()
    {
        var header = new Header();
        header.AddField(new HeaderField(HeaderFieldIdEnum.Username, "alice"));
        var original = new Message(header, "hello");
        var bytes = original.Serialize();

        var parser = new MessageParser();
        var messages = parser.ParseBytes(bytes);

        Assert.Single(messages);
        var reparsed = messages[0].Serialize();
        Assert.Equal(bytes, reparsed);
    }

    [Fact]
    public void ParseBytes_MultipleMessagesInSingleBuffer_ReturnsAllMessages()
    {
        var header1 = new Header();
        header1.AddField(new HeaderField(HeaderFieldIdEnum.Username, "a"));
        var m1 = new Message(header1, "one");

        var header2 = new Header();
        header2.AddField(new HeaderField(HeaderFieldIdEnum.Username, "b"));
        var m2 = new Message(header2, "two");

        var b1 = m1.Serialize();
        var b2 = m2.Serialize();
        var concat = new byte[b1.Length + b2.Length];
        Buffer.BlockCopy(b1, 0, concat, 0, b1.Length);
        Buffer.BlockCopy(b2, 0, concat, b1.Length, b2.Length);

        var parser = new MessageParser();
        var messages = parser.ParseBytes(concat);

        Assert.Equal(2, messages.Count);
        Assert.Equal(b1, messages[0].Serialize());
        Assert.Equal(b2, messages[1].Serialize());
    }

    [Fact]
    public void ParseBytes_PartialMessageAcrossCalls_ReturnsCombinedMessage()
    {
        var header = new Header();
        header.AddField(new HeaderField(HeaderFieldIdEnum.Username, "x"));
        var msg = new Message(header, "partial");
        var bytes = msg.Serialize();

        // split into two parts
        int split = bytes.Length / 2;
        var p1 = new byte[split];
        var p2 = new byte[bytes.Length - split];
        Buffer.BlockCopy(bytes, 0, p1, 0, p1.Length);
        Buffer.BlockCopy(bytes, p1.Length, p2, 0, p2.Length);

        var parser = new MessageParser();
        var m1 = parser.ParseBytes(p1);
        Assert.Empty(m1);

        var m2 = parser.ParseBytes(p2);
        Assert.Single(m2);
        Assert.Equal(bytes, m2[0].Serialize());
    }

    [Fact]
    public void ParseBytes_InvalidNegativeMessageLength_Throws()
    {
        // message length negative
        var buf = new byte[4 + 1];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), -1);
        buf[4] = 0x00;

        var parser = new MessageParser();
        Assert.Throws<InvalidDataException>(() => parser.ParseBytes(buf));
    }

    [Fact]
    public void ParseBytes_FailedDeserialize_Throws()
    {
        // message length = 1, but Message.Deserialize expects at least 4 bytes -> will return false and parser throws
        var buf = new byte[4 + 1];
        BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0,4), 1);
        buf[4] = 0x01;

        var parser = new MessageParser();
        Assert.Throws<InvalidDataException>(() => parser.ParseBytes(buf));
    }
}
