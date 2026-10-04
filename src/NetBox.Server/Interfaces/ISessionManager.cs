using NetBox.Server.Models;

namespace NetBox.Server.Interfaces;

internal interface ISessionManager
{
    public void Add(ClientSession session);
    public void Remove(ClientSession session);

    public bool TryAuthenticate(ClientSession session, string username);

    public bool TryGetByUsername(string username, out ClientSession? session);

    public IReadOnlyList<ClientSession> GetAll();
}