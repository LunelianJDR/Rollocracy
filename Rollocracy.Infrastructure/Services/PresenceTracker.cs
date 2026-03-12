using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Infrastructure.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        private readonly object _lock = new();

        private readonly Dictionary<string, PresenceConnection> _connectionsById = new();

        public void AddConnection(string connectionId, Guid sessionId, Guid playerSessionId, bool isGameMaster)
        {
            lock (_lock)
            {
                _connectionsById[connectionId] = new PresenceConnection
                {
                    ConnectionId = connectionId,
                    SessionId = sessionId,
                    PlayerSessionId = playerSessionId,
                    IsGameMaster = isGameMaster
                };
            }
        }

        public bool RemoveConnection(string connectionId, out Guid sessionId)
        {
            lock (_lock)
            {
                if (_connectionsById.TryGetValue(connectionId, out var connection))
                {
                    sessionId = connection.SessionId;
                    _connectionsById.Remove(connectionId);
                    return true;
                }

                sessionId = Guid.Empty;
                return false;
            }
        }

        public int GetConnectedPlayersCount(Guid sessionId)
        {
            lock (_lock)
            {
                return _connectionsById.Values
                    .Where(c => c.SessionId == sessionId && !c.IsGameMaster)
                    .Select(c => c.PlayerSessionId)
                    .Distinct()
                    .Count();
            }
        }

        public bool IsPlayerOnline(Guid playerSessionId)
        {
            lock (_lock)
            {
                return _connectionsById.Values.Any(c => c.PlayerSessionId == playerSessionId);
            }
        }

        private class PresenceConnection
        {
            public string ConnectionId { get; set; } = string.Empty;

            public Guid SessionId { get; set; }

            public Guid PlayerSessionId { get; set; }

            public bool IsGameMaster { get; set; }
        }
    }
}