using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.Interfaces;
using NetBox.Shared.ServerEvents;

namespace NetBox.Server.Handlers;

internal class LeaveChannelHandler : ICommandHandler
{
    private readonly ChannelManager _channelManager;

    public LeaveChannelHandler(ChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<IMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!LeaveChannelRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("Leave channel command but not valid request");
        }

        if (clientSession.Username == null)
        {
            var failResp = new LeaveChannelResponse(LeaveChannelResponseCode.FAILURE);
            await clientSession.SendMessage(failResp);
            return null;
        }

        if (clientSession.Channel == null)
        {
            var notIn = new LeaveChannelResponse(LeaveChannelResponseCode.NOT_IN_CHANNEL);
            await clientSession.SendMessage(notIn);
            return null;
        }

        if (_channelManager.TryGetChannel(clientSession.Channel, out var channel))
        {
            channel.RemoveMember(clientSession);

            if (channel.GetMembers().Count == 0)
            {
                _channelManager.TryRemoveChannel(channel.Name, out _);
            }
        }

        clientSession.SetChannel(null);

        var response = new LeaveChannelResponse(LeaveChannelResponseCode.SUCCESS);
        await clientSession.SendMessage(response);

        var leaveMessage = new LeaveMessage(clientSession.Username);
        return leaveMessage;
    }
}
