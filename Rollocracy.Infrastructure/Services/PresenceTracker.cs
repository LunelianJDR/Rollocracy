using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Infrastructure.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        private static readonly TimeSpan PresenceTtl = TimeSpan.FromSeconds(180);

        private readonly object _lock = new();

        private readonly Dictionary<string, PresenceConnection> _connectionsById = new();
        private readonly Dictionary<Guid, BrowserPresence> _presenceByPlayerSessionId = new();

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

                TouchPlayerPresenceInternal(sessionId, playerSessionId, isGameMaster);
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

        public bool RemovePlayerPresence(Guid sessionId, Guid playerSessionId)
        {
            lock (_lock)
            {
                var connectionIdsToRemove = _connectionsById.Values
                    .Where(c => c.SessionId == sessionId && c.PlayerSessionId == playerSessionId)
                    .Select(c => c.ConnectionId)
                    .ToList();

                foreach (var connectionId in connectionIdsToRemove)
                {
                    _connectionsById.Remove(connectionId);
                }

                return _presenceByPlayerSessionId.Remove(playerSessionId);
            }
        }

        public bool TouchPlayerPresence(Guid sessionId, Guid playerSessionId, bool isGameMaster)
        {
            lock (_lock)
            {
                CleanupExpiredPresenceInternal();

                var becameOnline = !_presenceByPlayerSessionId.ContainsKey(playerSessionId);

                TouchPlayerPresenceInternal(sessionId, playerSessionId, isGameMaster);

                return becameOnline;
            }
        }

        public int GetConnectedPlayersCount(Guid sessionId)
        {
            lock (_lock)
            {
                CleanupExpiredPresenceInternal();

                return _presenceByPlayerSessionId.Values
                    .Where(x => x.SessionId == sessionId && !x.IsGameMaster)
                    .Select(x => x.PlayerSessionId)
                    .Distinct()
                    .Count();
            }
        }

        public bool IsPlayerOnline(Guid playerSessionId)
        {
            lock (_lock)
            {
                CleanupExpiredPresenceInternal();

                return _presenceByPlayerSessionId.ContainsKey(playerSessionId);
            }
        }

        private void TouchPlayerPresenceInternal(Guid sessionId, Guid playerSessionId, bool isGameMaster)
        {
            _presenceByPlayerSessionId[playerSessionId] = new BrowserPresence
            {
                SessionId = sessionId,
                PlayerSessionId = playerSessionId,
                IsGameMaster = isGameMaster,
                LastSeenUtc = DateTime.UtcNow
            };
        }

        private void CleanupExpiredPresenceInternal()
        {
            var nowUtc = DateTime.UtcNow;

            var expiredPlayerSessionIds = _presenceByPlayerSessionId.Values
                .Where(x => nowUtc - x.LastSeenUtc > PresenceTtl)
                .Select(x => x.PlayerSessionId)
                .ToList();

            foreach (var playerSessionId in expiredPlayerSessionIds)
            {
                _presenceByPlayerSessionId.Remove(playerSessionId);

                var connectionIdsToRemove = _connectionsById.Values
                    .Where(c => c.PlayerSessionId == playerSessionId)
                    .Select(c => c.ConnectionId)
                    .ToList();

                foreach (var connectionId in connectionIdsToRemove)
                {
                    _connectionsById.Remove(connectionId);
                }
            }
        }

        private class PresenceConnection
        {
            public string ConnectionId { get; set; } = string.Empty;
            public Guid SessionId { get; set; }
            public Guid PlayerSessionId { get; set; }
            public bool IsGameMaster { get; set; }
        }

        private class BrowserPresence
        {
            public Guid SessionId { get; set; }
            public Guid PlayerSessionId { get; set; }
            public bool IsGameMaster { get; set; }
            public DateTime LastSeenUtc { get; set; }
        }
    }
}
