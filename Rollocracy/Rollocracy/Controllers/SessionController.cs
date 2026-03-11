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

        public SessionController(
            ISessionService sessionService,
            IHubContext<SessionHub> hub)
        {
            _sessionService = sessionService;
            _hub = hub;
        }

        // Retourne un joueur par son identifiant
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
    }
}