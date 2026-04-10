using Rollocracy.Domain.GameTests;

namespace Rollocracy.Domain.Interfaces
{
    public interface IGameTestService
    {
        Task<GameMasterActiveGameTestDto> CreateGameTestAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            GameTestCreateRequestDto request);

        Task<GameMasterActiveGameTestDto?> GetActiveGameTestForSessionAsync(Guid sessionId);

        Task<ActivePlayerGameTestDto?> GetActiveGameTestForPlayerAsync(Guid playerSessionId);

        Task<PlayerGameTestResultDto> RollForPlayerAsync(Guid playerSessionId, Guid gameTestId, bool isAutoRoll);

        Task AutoRollPendingAsync(Guid gameTestId);

        Task<List<SessionGameTestPresetDto>> GetSessionPresetsAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task SaveSessionPresetAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string presetName,
            GameTestCreateRequestDto request,
            bool overwrite);

        Task DeleteSessionPresetAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid presetId);

        Task RollbackLatestTestConsequencesOnlyAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task RollbackLatestTestAsync(Guid sessionId, Guid gameMasterUserAccountId);
    }
}