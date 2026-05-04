
namespace Rollocracy.Domain.Interfaces
{
    public interface ISessionNotifier
    {
        Task NotifyTestChangedAsync(Guid sessionId);

        Task NotifyCharacterStateChangedAsync(Guid sessionId);

        Task NotifyPresenceChangedAsync(Guid sessionId);

        Task NotifyPollChangedAsync(Guid sessionId);

        Task NotifyJournalChangedAsync(Guid sessionId);
        Task NotifyStoreChangedAsync(Guid sessionId);
    }
}
