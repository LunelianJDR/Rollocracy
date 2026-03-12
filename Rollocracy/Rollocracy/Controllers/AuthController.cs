using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IStringLocalizer _localizer;

        public AuthController(
            IAuthService authService,
            IStringLocalizerFactory localizerFactory)
        {
            _authService = authService;
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
                    request.IsGameMaster,
                    request.Language);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.IsGameMaster,
                    user.Language
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
            var success = await SignInUserAsync(request.Username, request.Password);

            if (!success)
                return Unauthorized(_localizer["Backend_InvalidCredentials"]);

            return Ok();
        }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> LoginForm([FromForm] LoginRequest request)
        {
            var success = await SignInUserAsync(request.Username, request.Password);

            if (!success)
                return Redirect("/login?error=1");

            return Redirect("/");
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
        public IActionResult Me()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;
            var isGameMaster = User.FindFirst("IsGameMaster")?.Value;
            var language = User.FindFirst("Language")?.Value;

            return Ok(new
            {
                UserId = userId,
                Username = username,
                IsGameMaster = isGameMaster,
                Language = language
            });
        }

        private async Task<bool> SignInUserAsync(string username, string password)
        {
            var user = await _authService.ValidateLoginAsync(username, password);

            if (user == null)
                return false;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("IsGameMaster", user.IsGameMaster.ToString()),
                new Claim("Language", user.Language)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            return true;
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsGameMaster { get; set; }
        public string Language { get; set; } = "fr";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}