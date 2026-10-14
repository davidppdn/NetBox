using NetBox.Shared.Protocols;
using NetBox.Server.Models;
using NetBox.Shared.Interfaces;

namespace NetBox.Server.Handlers;

internal interface ICommandHandler
{
    /// <summary>
    /// Handle an incoming message for a client session. Returns an IMessage to be optionally broadcast, or null.
    /// </summary>
    Task<IMessage?> Handle(Message message, ClientSession clientSession);
}
