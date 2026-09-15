using System;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols;

public class MessageParser
{
    private StringBuilder _stringBuilder = new StringBuilder();

    public List<Message> ParseBytes(byte[] data, int bytesRead)
    {
        var messages = new List<Message>();
        var stringData = Encoding.UTF8.GetString(data, 0, bytesRead);
        _stringBuilder.Append(stringData);

        while (true)
        {
            var indexOfNewLine = _stringBuilder.ToString().IndexOf('\n');

            if (indexOfNewLine == -1)
            {
                break;
            }

            var completeMessage = _stringBuilder.ToString(0, indexOfNewLine);
            messages.Add(new Message(completeMessage));
            _stringBuilder.Remove(0, indexOfNewLine + 1);
        }

        return messages;
    }
}
