using LostAndFound.Api.Services.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LostAndFound.Api.Services
{
    public class UserConnectionManager : IUserConnectionManager
    {
        private readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

        public bool AddConnection(int userId, string connectionId)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections)
            {
                var wasEmpty = connections.Count == 0;
                connections.Add(connectionId);
                return wasEmpty;
            }
        }

        public bool RemoveConnection(int userId, string connectionId)
        {
            if (!_userConnections.TryGetValue(userId, out var connections))
            {
                return false;
            }

            lock (connections)
            {
                if (!connections.Remove(connectionId))
                {
                    return false;
                }

                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<string> GetConnections(int userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    return connections.ToList();
                }
            }

            return Array.Empty<string>();
        }

        public IReadOnlyCollection<int> GetOnlineUsers()
        {
            return _userConnections.Keys.ToList();
        }

        public bool IsUserOnline(int userId)
        {
            return _userConnections.ContainsKey(userId);
        }
    }
}

