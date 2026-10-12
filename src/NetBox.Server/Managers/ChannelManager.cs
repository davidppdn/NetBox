using NetBox.Server.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NetBox.Server.Managers;

internal class ChannelManager
{
    private readonly ConcurrentDictionary<string, Channel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public bool TryCreateChannel(string name, out Channel? channel)
    {
        var newChannel = new Channel(name);

        if (_channels.TryAdd(name, newChannel))
        {
            channel = newChannel;
            return true;
        }

        channel = null;
        return false;
    }

    public bool TryGetChannel(string name, out Channel? channel)
    {
        return _channels.TryGetValue(name, out channel);
    }

    public bool TryRemoveChannel(string name, [NotNullWhen(true)] out IReadOnlyList<ClientSession>? members)
    {
        members = null;
        if (!_channels.TryRemove(name, out var removedChannel))
        {
            return false;
        }

        members = removedChannel.GetMembers();
        return true;
    }
}
