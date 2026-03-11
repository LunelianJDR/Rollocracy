using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Interfaces
{
    public interface ISessionService
    {
        Task<Session> CreateSessionAsync(
            Guid gameMasterUserAccountId,
            Guid gameSystemId,
            string sessionName,
            string sessionPassword);

        Task<PlayerSession> JoinSessionAsync(
            string gameMasterUsername,
            string sessionSlug,
            string sessionPassword,
            Guid userAccountId,
            string playerName,
            bool isGameMaster);

        Task<List<PlayerSession>> GetPlayersAsync(Guid sessionId);

        Task<Session?> GetSessionByOwnerAndSlugAsync(string gameMasterUsername, string sessionSlug);

        Task<Session?> GetSessionByIdAsync(Guid sessionId);

        Task<PlayerSession?> GetPlayerByIdAsync(Guid playerId);

        Task<List<Session>> GetSessionsByGameMasterAsync(Guid gameMasterUserAccountId);

        Task SetSessionActiveStateAsync(Guid sessionId, Guid gameMasterUserAccountId, bool isActive);

        Task<int> GetAliveCharacterCountAsync(Guid sessionId);

        // Associe un système à une session
        Task AssignGameSystemToSessionAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid gameSystemId);
    }
}