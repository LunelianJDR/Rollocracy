using Rollocracy.Domain.Account;

namespace Rollocracy.Domain.Interfaces
{
    public interface ITwitchAuthService
    {
        Task<string> CreateLoginChallengeUrlAsync(string language);

        Task<string> CreateLinkChallengeUrlAsync(Guid currentUserId, string language);

        Task<TwitchCallbackResultDto> HandleCallbackAsync(string code, string state);

        Task<TwitchPendingSessionDto> GetPendingSessionAsync(string pendingToken);

        Task<Guid> CompleteRegistrationAsync(CompleteTwitchRegistrationRequestDto request);

        Task<Guid> ConfirmMergeAsync(ConfirmTwitchMergeRequestDto request);

        Task LinkCurrentUserToTwitchAsync(Guid currentUserId, string pendingToken);

        Task UnlinkTwitchAsync(Guid currentUserId);

        Task RenameCurrentUserToTwitchLoginAsync(Guid currentUserId);
    }
}