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
            string playerName);

        Task<List<PlayerSession>> GetPlayersAsync(Guid sessionId);

        Task<Session?> GetSessionByOwnerAndSlugAsync(string gameMasterUsername, string sessionSlug);

        Task<Session?> GetSessionByIdAsync(Guid sessionId);

        Task<PlayerSession?> GetPlayerByIdAsync(Guid playerId);

        Task<List<Session>> GetSessionsByGameMasterAsync(Guid gameMasterUserAccountId);

        Task SetSessionActiveStateAsync(Guid sessionId, Guid gameMasterUserAccountId, bool isActive);

        Task<int> GetAliveCharacterCountAsync(Guid sessionId);

        Task AssignGameSystemToSessionAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid gameSystemId);

        Task<bool> CanUserCreateSessionsAsync(Guid userAccountId);

        Task<int> GetUserMaxPlayersPerSessionAsync(Guid userAccountId);

        Task<SessionSettingsDto?> GetSessionSettingsAsync(Guid sessionId, Guid gameMasterUserAccountId, string baseUri);

        Task<Session> UpdateSessionSettingsAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string sessionName,
            string sessionPassword,
            bool updateJoinUrlSlug);
    }
}