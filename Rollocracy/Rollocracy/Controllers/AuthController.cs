using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Account;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAccountSecurityService _accountSecurityService;
        private readonly ITwitchAuthService _twitchAuthService;
        private readonly IStringLocalizer _localizer;

        public AuthController(
            IAuthService authService,
            IAccountSecurityService accountSecurityService,
            ITwitchAuthService twitchAuthService,
            IStringLocalizerFactory localizerFactory)
        {
            _authService = authService;
            _accountSecurityService = accountSecurityService;
            _twitchAuthService = twitchAuthService;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(
                    request.Username,
                    request.Password,
                    request.Email,
                    request.Language);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Language,
                    user.WantsToBeGameMaster,
                    user.MaxPlayersPerSession
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var success = await SignInUserAsync(request.Username, request.Password, request.RememberMe);

            if (!success)
                return Unauthorized(_localizer["Auth_InvalidCredentials"]);

            return Ok();
        }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> LoginForm([FromForm] LoginRequest request)
        {
            var success = await SignInUserAsync(request.Username, request.Password, request.RememberMe);

            if (!success)
                return Redirect("/login?error=1");

            return Redirect("/");
        }

        // Entrée publique : connexion directe avec Twitch
        [HttpGet("/auth/twitch/login")]
        public async Task<IActionResult> TwitchLogin([FromQuery] string? language = null)
        {
            var url = await _twitchAuthService.CreateLoginChallengeUrlAsync(language ?? "fr");
            return Redirect(url);
        }

        // Entrée authentifiée : liaison Twitch depuis un compte local déjà connecté
        [Authorize]
        [HttpGet("/auth/twitch/link")]
        public async Task<IActionResult> TwitchLink()
        {
            var userId = GetCurrentUserId();

            var currentLanguage = User.FindFirst("Language")?.Value ?? "fr";
            var url = await _twitchAuthService.CreateLinkChallengeUrlAsync(userId, currentLanguage);

            return Redirect(url);
        }

        // Callback unique Twitch
        [HttpGet("/auth/twitch/callback")]
        public async Task<IActionResult> TwitchCallback([FromQuery] string? code, [FromQuery] string? state)
        {
            try
            {
                var result = await _twitchAuthService.HandleCallbackAsync(code ?? string.Empty, state ?? string.Empty);

                switch (result.Outcome)
                {
                    case "sign_in":
                        if (!result.UserAccountId.HasValue)
                            return Redirect("/login?twitch_error=1");

                        var user = await _authService.GetUserByIdAsync(result.UserAccountId.Value);
                        if (user == null)
                            return Redirect("/login?twitch_error=1");

                        await SignInUserAsync(user, rememberMe: true);
                        return Redirect("/");

                    case "redirect_merge":
                        return Redirect($"/twitch/merge?token={Uri.EscapeDataString(result.PendingToken ?? string.Empty)}");

                    case "redirect_complete":
                        return Redirect($"/twitch/complete?token={Uri.EscapeDataString(result.PendingToken ?? string.Empty)}");

                    case "redirect_account":
                        return Redirect("/account/settings?twitch_linked=1");

                    default:
                        return Redirect("/login?twitch_error=1");
                }
            }
            catch
            {
                return Redirect("/login?twitch_error=1");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                await _accountSecurityService.RequestPasswordResetAsync(request.EmailOrUsername);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmNewPassword)
                    return BadRequest(_localizer["Backend_NewPasswordsDoNotMatch"]);

                await _accountSecurityService.ResetPasswordAsync(request.Token, request.NewPassword);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                await _accountSecurityService.VerifyEmailAsync(token);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/auth/logout")]
        public async Task<IActionResult> LogoutPage()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                IsTwitchLinked = user.IsTwitchLinked,
                TwitchUserId = user.TwitchUserId,
                TwitchLogin = user.TwitchLogin,
                TwitchDisplayName = user.TwitchDisplayName,
                WantsToBeGameMaster = user.WantsToBeGameMaster,
                MaxPlayersPerSession = user.MaxPlayersPerSession,
                IsGameMaster = user.IsGameMaster,
                Language = user.Language
            });
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                throw new UnauthorizedAccessException();

            return userId;
        }

        private async Task<bool> SignInUserAsync(string username, string password, bool rememberMe)
        {
            var user = await _authService.ValidateLoginAsync(username, password);

            if (user == null)
                return false;

            await SignInUserAsync(user, rememberMe);
            return true;
        }

        private async Task SignInUserAsync(UserAccount user, bool rememberMe)
        {
            var claims = BuildClaims(user);

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true
            };

            if (rememberMe)
            {
                authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        private static List<Claim> BuildClaims(UserAccount user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("IsGameMaster", user.IsGameMaster.ToString()),
                new Claim("Language", user.Language),
                new Claim("HasEmail", (!string.IsNullOrWhiteSpace(user.Email)).ToString()),
                new Claim("WantsToBeGameMaster", user.WantsToBeGameMaster.ToString()),
                new Claim("IsTwitchLinked", user.IsTwitchLinked.ToString())
            };
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Language { get; set; } = "fr";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
