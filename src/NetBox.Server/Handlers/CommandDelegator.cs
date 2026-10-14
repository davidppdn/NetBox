using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Interfaces;

namespace NetBox.Server.Handlers;

internal class CommandDelegator
{
    private readonly CommandHandlerFactory _factory;

    public CommandDelegator(CommandHandlerFactory factory)
    {
        _factory = factory;
    }

    public async Task<IMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!_factory.TryGetHandler(message.Command, out var handler) || handler == null)
        {
            throw new InvalidDataException($"No handler for command: {message.Command}");
        }

        return await handler.Handle(message, clientSession);
    }
}
