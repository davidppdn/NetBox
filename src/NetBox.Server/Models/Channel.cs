using NetBox.Server.Models;

internal class Channel(string name)
{
    private readonly List<ClientSession> _members = new();
    private readonly object _membersLock = new();

    public string Name { get; } = name;

    public IReadOnlyList<ClientSession> GetMembers()
    {
        lock (_membersLock)
        {
            return _members;
        }
    }

    public void AddMember(ClientSession session)
    {
        lock (_membersLock)
        {
            if (!_members.Contains(session))
            {
                _members.Add(session);
            }
        }
    }

    public void RemoveMember(ClientSession session)
    {
        lock (_membersLock)
        {
            _members.Remove(session);
        }
    }
}