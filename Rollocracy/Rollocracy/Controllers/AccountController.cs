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
    [Authorize]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;

        public AccountController(
            IAccountService accountService,
            IAuthService authService)
        {
            _accountService = accountService;
            _authService = authService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var userId = GetCurrentUserId();
            var dto = await _accountService.GetAccountSettingsAsync(userId);
            return Ok(dto);
        }

        [HttpPut("general")]
        public async Task<IActionResult> UpdateGeneral([FromBody] UpdateAccountGeneralRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();

                await _accountService.UpdateGeneralInfoAsync(
                    userId,
                    request.Email,
                    request.Language);

                var updatedUser = await _authService.GetUserByIdAsync(userId);
                if (updatedUser != null)
                    await RefreshSignInAsync(updatedUser);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("identity/username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateAccountUsernameRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();

                await _accountService.UpdateUsernameAsync(userId, request.NewUsername);

                var updatedUser = await _authService.GetUserByIdAsync(userId);
                if (updatedUser != null)
                    await RefreshSignInAsync(updatedUser);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("mj-mode")]
        public async Task<IActionResult> UpdateGameMasterMode([FromBody] UpdateAccountGameMasterModeRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();

                await _accountService.SetGameMasterModeAsync(userId, request.Enabled);

                var updatedUser = await _authService.GetUserByIdAsync(userId);
                if (updatedUser != null)
                    await RefreshSignInAsync(updatedUser);

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

        private async Task RefreshSignInAsync(UserAccount user)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("IsGameMaster", user.IsGameMaster.ToString()),
                new Claim("Language", user.Language),
                new Claim("HasEmail", (!string.IsNullOrWhiteSpace(user.Email)).ToString()),
                new Claim("WantsToBeGameMaster", user.WantsToBeGameMaster.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authenticateResult.Properties);
        }
    }
}