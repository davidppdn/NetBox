using NetBox.Server.Managers;
using NetBox.Server.Models;
using NetBox.Shared.Protocols;
using NetBox.Shared.Protocols.Enums;
using NetBox.Shared.Requests;
using NetBox.Shared.Responses;
using NetBox.Shared.Interfaces;

namespace NetBox.Server.Handlers;

internal class GetChannelListHandler : ICommandHandler
{
    private readonly ChannelManager _channelManager;

    public GetChannelListHandler(ChannelManager channelManager)
    {
        _channelManager = channelManager;
    }

    public async Task<IMessage?> Handle(Message message, ClientSession clientSession)
    {
        if (!GetChannelListRequest.FromMessage(message, out var request))
        {
            throw new InvalidDataException("GetChannelList command but not valid request");
        }

        var names = _channelManager.GetAllChannelNames();
        var response = new GetChannelListResponse(GetChannelListResponseCode.SUCCESS, names);
        await clientSession.SendMessage(response);
        return null;
    }
}
