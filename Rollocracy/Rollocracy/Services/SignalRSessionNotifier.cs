using Microsoft.AspNetCore.SignalR;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;

namespace Rollocracy.Services
{
    public class SignalRSessionNotifier : ISessionNotifier
    {
        private readonly IHubContext<SessionHub> _hubContext;

        public SignalRSessionNotifier(IHubContext<SessionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyTestChangedAsync(Guid sessionId)
        {
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("TestChanged");
        }

        public async Task NotifyCharacterStateChangedAsync(Guid sessionId)
        {
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("CharacterStateChanged");
        }

        public async Task NotifyPresenceChangedAsync(Guid sessionId)
        {
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("PresenceChanged");
        }

        public async Task NotifyPollChangedAsync(Guid sessionId)
        {
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("PollChanged");
        }
    }
}