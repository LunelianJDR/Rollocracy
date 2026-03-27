using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rollocracy.Domain.Account;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Controllers
{
    [ApiController]
    [Route("api/twitch")]
    public class TwitchController : ControllerBase
    {
        private readonly ITwitchAuthService _twitchAuthService;
        private readonly IAuthService _authService;

        public TwitchController(
            ITwitchAuthService twitchAuthService,
            IAuthService authService)
        {
            _twitchAuthService = twitchAuthService;
            _authService = authService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] string token)
        {
            try
            {
                var dto = await _twitchAuthService.GetPendingSessionAsync(token);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] CompleteTwitchRegistrationRequestDto request)
        {
            try
            {
                var userId = await _twitchAuthService.CompleteRegistrationAsync(request);

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                    await RefreshSignInAsync(user, true);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("merge")]
        public async Task<IActionResult> Merge([FromBody] ConfirmTwitchMergeRequestDto request)
        {
            try
            {
                var userId = await _twitchAuthService.ConfirmMergeAsync(request);

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                    await RefreshSignInAsync(user, true);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("rename-current-to-twitch")]
        public async Task<IActionResult> RenameCurrentToTwitch()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _twitchAuthService.RenameCurrentUserToTwitchLoginAsync(userId);

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                    await RefreshSignInAsync(user, true);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("unlink-current")]
        public async Task<IActionResult> UnlinkCurrent()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _twitchAuthService.UnlinkTwitchAsync(userId);

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                    await RefreshSignInAsync(user, true);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                throw new UnauthorizedAccessException();

            return userId;
        }

        private async Task RefreshSignInAsync(UserAccount user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("IsGameMaster", user.IsGameMaster.ToString()),
                new Claim("Language", user.Language),
                new Claim("HasEmail", (!string.IsNullOrWhiteSpace(user.Email)).ToString()),
                new Claim("WantsToBeGameMaster", user.WantsToBeGameMaster.ToString()),
                new Claim("IsTwitchLinked", user.IsTwitchLinked.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }
    }
}