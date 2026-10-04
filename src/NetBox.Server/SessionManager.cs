using NetBox.Server.Interfaces;
using NetBox.Server.Models;
using System.Collections.Concurrent;

namespace NetBox.Server;

internal class SessionManager : ISessionManager
{
    private readonly List<ClientSession> _connectedClients = new();
    private readonly ConcurrentDictionary<string, ClientSession> _authenticatedSessions = new();
    private readonly object _clientsLock = new();

    public void Add(ClientSession session)
    {
        lock(_clientsLock)
        {
            _connectedClients.Add(session);
        }
    }

    public void Remove(ClientSession session)
    {
        if (session.Username != null)
        {
            _authenticatedSessions.TryRemove(session.Username, out _);
        }

        lock (_clientsLock)
        {
            _connectedClients.Remove(session);
        }
    }

    public bool TryAuthenticate(ClientSession session, string username)
    {
        var success = _authenticatedSessions.TryAdd(username, session);
        if (success)
        {
            session.SetUsername(username);
        }

        return success;
    }

    public bool TryGetByUsername(string username, out ClientSession? session)
    {
        return _authenticatedSessions.TryGetValue(username, out session);
    }

    public IReadOnlyList<ClientSession> GetAll()
    {
        lock(_clientsLock)
        {
            return _connectedClients.ToList();
        }
    }
}
