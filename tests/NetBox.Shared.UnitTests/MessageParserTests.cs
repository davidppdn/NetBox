using System.Text;
using NetBox.Shared.Protocols;
using Xunit;

namespace NetBox.Shared.UnitTests;

public class MessageParserTests
{
    [Fact]
    public void ParseBytes_SingleCompleteMessage_ReturnsMessage()
    {
        var parser = new MessageParser();
        var data = Encoding.UTF8.GetBytes("hello\n");
        var messages = parser.ParseBytes(data, data.Length);
        Assert.Single(messages);
        Assert.Equal("hello", messages[0].Content);
    }

    [Fact]
    public void ParseBytes_MultipleMessagesInSingleBuffer_ReturnsAllMessages()
    {
        var parser = new MessageParser();
        var data = Encoding.UTF8.GetBytes("one\nTwo\n");
        var messages = parser.ParseBytes(data, data.Length);
        Assert.Equal(2, messages.Count);
        Assert.Equal("one", messages[0].Content);
        Assert.Equal("Two", messages[1].Content);
    }

    [Fact]
    public void ParseBytes_PartialMessageAcrossCalls_ReturnsCombinedMessage()
    {
        var parser = new MessageParser();
        var part1 = Encoding.UTF8.GetBytes("part");
        var messages1 = parser.ParseBytes(part1, part1.Length);
        Assert.Empty(messages1);

        var part2 = Encoding.UTF8.GetBytes("ial\n");
        var messages2 = parser.ParseBytes(part2, part2.Length);
        Assert.Single(messages2);
        Assert.Equal("partial", messages2[0].Content);
    }

    [Fact]
    public void ParseBytes_NoNewLine_ReturnsEmpty()
    {
        var parser = new MessageParser();
        var data = Encoding.UTF8.GetBytes("incomplete");
        var messages = parser.ParseBytes(data, data.Length);
        Assert.Empty(messages);
    }

    [Fact]
    public void ParseBytes_OneCompleteAndOnePartialMessage_ReturnsCompleteMessage()
    {
        var parser = new MessageParser();
        var data = Encoding.UTF8.GetBytes("complete\npartial");
        var messages = parser.ParseBytes(data, data.Length);
        Assert.Single(messages);
        Assert.Equal("complete", messages[0].Content);
    }
}
