using NetBox.Shared.Protocols;

namespace NetBox.Shared.Interfaces;

public interface IMessage
{
    public Message ToMessage();
}
