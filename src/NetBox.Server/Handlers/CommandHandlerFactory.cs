using NetBox.Server.Managers;
using NetBox.Server.Interfaces;
using NetBox.Shared.Protocols.Enums;

namespace NetBox.Server.Handlers;

internal class CommandHandlerFactory
{
    private readonly Dictionary<Command, ICommandHandler> _handlers = new();

    public CommandHandlerFactory(ISessionManager sessionManager, ChannelManager channelManager)
    {
        // Handlers that need session manager
        _handlers[Command.LOGIN] = new LoginHandler(sessionManager);

        // Channel related handlers
        _handlers[Command.CREATE_CHANNEL] = new CreateChannelHandler(channelManager);
        _handlers[Command.GET_CHANNEL_LIST] = new GetChannelListHandler(channelManager);
        _handlers[Command.JOIN_CHANNEL] = new JoinChannelHandler(channelManager);
        _handlers[Command.LEAVE_CHANNEL] = new LeaveChannelHandler(channelManager);

        // Message handler
        _handlers[Command.SEND_MESSAGE] = new SendMessageHandler();
    }

    public bool TryGetHandler(Command command, out ICommandHandler? handler)
    {
        return _handlers.TryGetValue(command, out handler);
    }
}
