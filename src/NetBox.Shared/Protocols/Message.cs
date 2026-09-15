using System;
using System.Collections.Generic;
using System.Text;

namespace NetBox.Shared.Protocols;

public record Message(string Content)
{
    public byte[] ToBytes() 
        => Encoding.UTF8.GetBytes(Content + "\n");
}
