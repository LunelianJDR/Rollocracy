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

        Task RollbackLatestTestAsync(Guid sessionId, Guid gameMasterUserAccountId);
    }
}