using NetBox.Server.Interfaces;
using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.Interfaces;

namespace NetBox.Server.Handlers;

internal class CreateChannelHandler : ICommandHandler
{
    private readonly ChannelManager _channelManager;

    public CreateChannelHandler(ChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    // Ensure method is public for consistency.
    public async Task<IMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!CreateChannelRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Create channel command but not valid request");
        }

        if (clientSession.Username == null)
        {
            var fail = new CreateChannelResponse(CreateChannelResponseCode.CHANNEL_ALREADY_EXISTS);
            await clientSession.SendMessage(fail);
            return null;
        }

        var success = _channelManager.TryCreateChannel(request.ChannelName, out var channel);
        var response = new CreateChannelResponse(success ? CreateChannelResponseCode.CHANNEL_CREATED : CreateChannelResponseCode.CHANNEL_ALREADY_EXISTS);
        await clientSession.SendMessage(response);
        return null;
    }
}
