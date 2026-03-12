using Microsoft.AspNetCore.SignalR;
using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Hubs
{
    public class SessionHub : Hub
    {
        private readonly IPresenceTracker _presenceTracker;

        public SessionHub(IPresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
        }

        // Groupe utilisé par la page MJ et les pages joueur pour recevoir les mises à jour temps réel
        public async Task JoinSessionGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        // Présence d'un joueur réellement "dans la session"
        public async Task JoinPlayerPresence(string sessionId, string playerSessionId, bool isGameMaster)
        {
            var parsedSessionId = Guid.Parse(sessionId);
            var parsedPlayerSessionId = Guid.Parse(playerSessionId);

            _presenceTracker.AddConnection(
                Context.ConnectionId,
                parsedSessionId,
                parsedPlayerSessionId,
                isGameMaster);

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            await Clients.Group(sessionId).SendAsync("PresenceChanged");
        }

        // Sortie explicite de présence depuis le client joueur avant fermeture de la connexion
        public async Task LeavePlayerPresence(string sessionId, string playerSessionId)
        {
            if (!Guid.TryParse(sessionId, out var parsedSessionId))
                return;

            var removed = _presenceTracker.RemoveConnection(Context.ConnectionId, out var removedSessionId);

            if (removed)
            {
                var targetSessionId = removedSessionId != Guid.Empty
                    ? removedSessionId
                    : parsedSessionId;

                await Clients.Group(targetSessionId.ToString()).SendAsync("PresenceChanged");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_presenceTracker.RemoveConnection(Context.ConnectionId, out var sessionId) && sessionId != Guid.Empty)
            {
                await Clients.Group(sessionId.ToString()).SendAsync("PresenceChanged");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}