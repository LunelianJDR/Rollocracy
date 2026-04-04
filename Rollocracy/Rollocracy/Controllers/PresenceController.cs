using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;

namespace Rollocracy.Controllers
{
    [ApiController]
    [Route("api/presence")]
    public class PresenceController : ControllerBase
    {
        private readonly IPresenceTracker _presenceTracker;
        private readonly IHubContext<SessionHub> _hubContext;

        public PresenceController(
            IPresenceTracker presenceTracker,
            IHubContext<SessionHub> hubContext)
        {
            _presenceTracker = presenceTracker;
            _hubContext = hubContext;
        }

        [HttpPost("heartbeat")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Heartbeat([FromBody] PresenceHeartbeatRequest request)
        {
            if (request.SessionId == Guid.Empty || request.PlayerSessionId == Guid.Empty)
                return BadRequest();

            _presenceTracker.TouchPlayerPresence(
                request.SessionId,
                request.PlayerSessionId,
                request.IsGameMaster);

            await _hubContext.Clients.Group(request.SessionId.ToString()).SendAsync("PresenceChanged");

            return Ok();
        }

        [HttpPost("disconnect")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Disconnect([FromBody] PresenceDisconnectRequest request)
        {
            if (request.SessionId == Guid.Empty || request.PlayerSessionId == Guid.Empty)
                return BadRequest();

            var removed = _presenceTracker.RemovePlayerPresence(request.SessionId, request.PlayerSessionId);

            if (removed)
            {
                await _hubContext.Clients.Group(request.SessionId.ToString()).SendAsync("PresenceChanged");
            }

            return Ok();
        }

        public sealed class PresenceHeartbeatRequest
        {
            public Guid SessionId { get; set; }
            public Guid PlayerSessionId { get; set; }
            public bool IsGameMaster { get; set; }
        }

        public sealed class PresenceDisconnectRequest
        {
            public Guid SessionId { get; set; }
            public Guid PlayerSessionId { get; set; }
        }
    }
}