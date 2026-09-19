using System;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols;

public class Message
{
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
}
