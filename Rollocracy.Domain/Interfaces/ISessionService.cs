using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;

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

        Task<List<PlayerResumableSessionDto>> GetResumablePlayerSessionsAsync(Guid userAccountId);

        Task<Guid> JoinSessionAsync(Guid userAccountId, string gameMasterUsername, string sessionSlug, string? sessionPassword);

        Task SetSessionActiveStateAsync(Guid sessionId, Guid gameMasterUserAccountId, bool isActive);

        Task DeleteSessionAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task<int> GetAliveCharacterCountAsync(Guid sessionId);

        Task<List<PlayerSession>> GetEligibleSpecialRolePlayersAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task<List<PlayerSession>> GetPlayersWithSpecialRolesAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task AssignSpecialRoleAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid playerSessionId, SessionSpecialRole role);

        Task RemoveSpecialRoleAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid playerSessionId);

        Task AssignGameSystemToSessionAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid gameSystemId);

        Task<bool> CanUserCreateSessionsAsync(Guid userAccountId);

        Task<int> GetUserMaxPlayersPerSessionAsync(Guid userAccountId);

        Task<SessionSettingsDto?> GetSessionSettingsAsync(Guid sessionId, Guid gameMasterUserAccountId, string baseUri);

        Task<SessionJournalEditorDto?> GetSessionJournalEditorAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task SaveSessionJournalAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            SessionJournalSaveRequestDto request);

        Task<SessionJournalViewDto?> GetPublicSessionJournalAsync(Guid playerSessionId);

        Task<List<SessionGaugeDto>> GetSessionGaugesAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task<List<SessionGaugeDto>> GetVisibleSessionGaugesAsync(Guid sessionId, Guid userAccountId);

        Task UpdateSessionGaugeCurrentValueAsync(
            Guid sessionId,
            Guid userAccountId,
            Guid sessionGaugeId,
            int currentValue);

        Task<SessionGauge> CreateSessionGaugeAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int currentValue);

        Task UpdateSessionGaugeAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            Guid sessionGaugeId,
            string name,
            int minValue,
            int maxValue,
            int currentValue);

        Task DeleteSessionGaugeAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid sessionGaugeId);

        Task<List<EditableItemDefinitionDto>> GetSessionItemDefinitionsAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task SaveSessionItemDefinitionsAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            List<EditableItemDefinitionDto> requestItems);
        Task<SessionStoreEditorDto?> GetSessionStoreEditorAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task SaveSessionStoreAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            SessionStoreEditorDto request);

        Task<PlayerSessionStoreDto> GetPlayerSessionStoreAsync(Guid playerSessionId);


        Task<Session> UpdateSessionSettingsAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string sessionName,
            string sessionPassword,
            bool updateJoinUrlSlug);
    }
}
