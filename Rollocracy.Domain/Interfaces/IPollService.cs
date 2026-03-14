using Rollocracy.Domain.Polls;

namespace Rollocracy.Domain.Interfaces
{
    public interface IPollService
    {
        Task<PollForGameMasterDto> CreatePollAsync(Guid sessionId, Guid gameMasterUserAccountId, PollCreateRequestDto request);

        Task<PollForGameMasterDto?> GetLatestPollForGameMasterAsync(Guid sessionId);

        Task<PollForPlayerDto?> GetLatestPollForPlayerAsync(Guid playerSessionId);

        Task<List<PollForGameMasterDto>> GetRecentPollsForGameMasterAsync(Guid sessionId, int takeCount);

        Task<PollForGameMasterDto?> GetPollByIdForGameMasterAsync(Guid sessionId, Guid pollId);

        Task VoteAsync(Guid playerSessionId, Guid pollId, Guid optionId);

        Task ClosePollAsync(Guid sessionId, Guid gameMasterUserAccountId);

        Task UndoLatestPollConsequencesAsync(Guid sessionId, Guid gameMasterUserAccountId);
    }
}
