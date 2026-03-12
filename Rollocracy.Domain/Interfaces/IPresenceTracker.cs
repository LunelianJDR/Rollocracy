namespace Rollocracy.Domain.Interfaces
{
    public interface IPresenceTracker
    {
        void AddConnection(string connectionId, Guid sessionId, Guid playerSessionId, bool isGameMaster);

        bool RemoveConnection(string connectionId, out Guid sessionId);

        int GetConnectedPlayersCount(Guid sessionId);

        bool IsPlayerOnline(Guid playerSessionId);
    }
}