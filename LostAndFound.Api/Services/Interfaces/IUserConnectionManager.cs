using System.Collections.Generic;

namespace LostAndFound.Api.Services.Interfaces
{
    public interface IUserConnectionManager
    {
        bool AddConnection(int userId, string connectionId);
        bool RemoveConnection(int userId, string connectionId);
        IReadOnlyCollection<string> GetConnections(int userId);
        IReadOnlyCollection<int> GetOnlineUsers();
        bool IsUserOnline(int userId);
    }
}

