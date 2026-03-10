using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Hubs;

namespace Rollocracy.Controllers
{
    [ApiController]
    [Route("api/session")]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IHubContext<SessionHub> _hub;

        public SessionController(ISessionService sessionService,IHubContext<SessionHub> hub)
        {
            _sessionService = sessionService;
            _hub = hub;
        }

        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> GetPlayer(Guid playerId)
        {
            try
            {
                var player = await _sessionService.GetPlayerByIdAsync(playerId);

                if (player == null)
                    return NotFound();

                return Ok(player);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinSession([FromBody] JoinSessionRequest request)
        {
            try
            {
                var player = await _sessionService.JoinSessionAsync(request.SessionCode, request.PlayerName);

                await _hub.Clients
                    .Group(player.SessionId.ToString())
                    .SendAsync("PlayerJoined", player);

                return Ok(player);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }

    public class JoinSessionRequest
    {
        public string SessionCode { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;
    }
}