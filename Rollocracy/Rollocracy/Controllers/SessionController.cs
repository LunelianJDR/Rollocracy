using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer _localizer;

        public SessionController(
            ISessionService sessionService,
            IHubContext<SessionHub> hub,
            IStringLocalizerFactory localizerFactory)
        {
            _sessionService = sessionService;
            _hub = hub;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> GetPlayer(Guid playerId)
        {
            try
            {
                var player = await _sessionService.GetPlayerByIdAsync(playerId);

                if (player == null)
                    return NotFound(_localizer["Backend_PlayerNotFound"]);

                return Ok(player);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}