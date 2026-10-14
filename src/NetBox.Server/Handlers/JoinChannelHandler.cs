using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.Interfaces;

namespace NetBox.Server.Handlers;

internal class JoinChannelHandler : ICommandHandler
{
    private readonly ChannelManager _channelManager;

    public JoinChannelHandler(ChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<IMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!JoinChannelRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Join channel command but not valid request");
        }

        if (clientSession.Username == null)
        {
            var failResp = new JoinChannelResponse(JoinChannelResponseCode.FAILURE);
            await clientSession.SendMessage(failResp);
            return null;
        }

        if (clientSession.CurrentChannel != null)
        {
            var already = new JoinChannelResponse(JoinChannelResponseCode.ALREADY_IN_CHANNEL);
            await clientSession.SendMessage(already);
            return null;
        }

        if (!_channelManager.TryGetChannel(request.ChannelName, out var channel))
        {
            var notFound = new JoinChannelResponse(JoinChannelResponseCode.CHANNEL_NOT_FOUND);
            await clientSession.SendMessage(notFound);
            return null;
        }

        channel.AddMember(clientSession);
        clientSession.SetChannel(channel.Name);

        var response = new JoinChannelResponse(JoinChannelResponseCode.SUCCESS);
        await clientSession.SendMessage(response);
        return null;
    }
}
