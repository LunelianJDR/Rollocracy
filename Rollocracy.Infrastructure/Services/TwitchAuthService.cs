using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Rollocracy.Domain.Account;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Options;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class TwitchAuthService : ITwitchAuthService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IAuthService _authService;
        private readonly IStringLocalizer _localizer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TwitchOptions _options;

        public TwitchAuthService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IAuthService authService,
            IStringLocalizerFactory localizerFactory,
            IHttpClientFactory httpClientFactory,
            IOptions<TwitchOptions> options)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<string> CreateLoginChallengeUrlAsync(string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = new TwitchPendingAuthSession
            {
                Id = Guid.NewGuid(),
                PublicToken = CreateToken(),
                OAuthState = CreateToken(),
                FlowType = "login",
                Language = NormalizeLanguage(language),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(20)
            };

            context.Set<TwitchPendingAuthSession>().Add(pending);
            await context.SaveChangesAsync();

            return BuildAuthorizeUrl(pending.OAuthState);
        }

        public async Task<string> CreateLinkChallengeUrlAsync(Guid currentUserId, string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = new TwitchPendingAuthSession
            {
                Id = Guid.NewGuid(),
                PublicToken = CreateToken(),
                OAuthState = CreateToken(),
                FlowType = "link",
                CurrentUserAccountId = currentUserId,
                Language = NormalizeLanguage(language),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(20)
            };

            context.Set<TwitchPendingAuthSession>().Add(pending);
            await context.SaveChangesAsync();

            return BuildAuthorizeUrl(pending.OAuthState);
        }

        public async Task<TwitchCallbackResultDto> HandleCallbackAsync(string code, string state)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = await context.Set<TwitchPendingAuthSession>()
                .FirstOrDefaultAsync(x =>
                    x.OAuthState == state &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (pending == null)
                throw new Exception(_localizer["Backend_TwitchStateInvalidOrExpired"]);

            var twitchProfile = await ExchangeCodeAndGetUserAsync(code);

            pending.TwitchUserId = twitchProfile.Id;
            pending.TwitchLogin = twitchProfile.Login;
            pending.TwitchDisplayName = twitchProfile.DisplayName;
            pending.TwitchEmail = string.IsNullOrWhiteSpace(twitchProfile.Email)
                ? null
                : twitchProfile.Email.Trim().ToLowerInvariant();

            // Cas 1 : compte déjà lié à ce Twitch user => connexion directe
            var linkedAccount = await _authService.GetUserByTwitchUserIdAsync(twitchProfile.Id);
            if (linkedAccount != null)
            {
                pending.ConsumedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();

                return new TwitchCallbackResultDto
                {
                    Outcome = "sign_in",
                    UserAccountId = linkedAccount.Id
                };
            }

            // Cas 2 : liaison depuis une session déjà connectée
            if (pending.FlowType == "link" && pending.CurrentUserAccountId.HasValue)
            {
                await _authService.LinkTwitchToExistingAccountAsync(
                    pending.CurrentUserAccountId.Value,
                    twitchProfile.Id,
                    twitchProfile.Login,
                    twitchProfile.DisplayName,
                    twitchProfile.Email);

                pending.ConsumedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();

                return new TwitchCallbackResultDto
                {
                    Outcome = "redirect_account"
                };
            }

            // Cas 3 : si Twitch fournit un email qui matche un compte local existant, on demande confirmation de fusion
            if (!string.IsNullOrWhiteSpace(pending.TwitchEmail))
            {
                var matchedByEmail = await _authService.GetUserByEmailAsync(pending.TwitchEmail);

                if (matchedByEmail != null)
                {
                    pending.FlowType = "merge";
                    pending.MatchedUserAccountId = matchedByEmail.Id;

                    await context.SaveChangesAsync();

                    return new TwitchCallbackResultDto
                    {
                        Outcome = "redirect_merge",
                        PendingToken = pending.PublicToken
                    };
                }
            }

            // Cas 4 : pseudo Twitch disponible => création automatique
            var suggestedUsername = pending.TwitchLogin ?? twitchProfile.DisplayName ?? string.Empty;
            var usernameAvailable = await _authService.IsUsernameAvailableAsync(suggestedUsername);

            if (usernameAvailable)
            {
                var createdUser = await _authService.CreateTwitchAccountAsync(
                    suggestedUsername,
                    pending.TwitchEmail,
                    pending.Language,
                    twitchProfile.Id,
                    twitchProfile.Login,
                    twitchProfile.DisplayName);

                pending.ConsumedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();

                return new TwitchCallbackResultDto
                {
                    Outcome = "sign_in",
                    UserAccountId = createdUser.Id
                };
            }

            // Cas 5 : pseudo indisponible ou besoin de complétion
            pending.FlowType = "complete";
            await context.SaveChangesAsync();

            return new TwitchCallbackResultDto
            {
                Outcome = "redirect_complete",
                PendingToken = pending.PublicToken
            };
        }

        public async Task<TwitchPendingSessionDto> GetPendingSessionAsync(string pendingToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = await context.Set<TwitchPendingAuthSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PublicToken == pendingToken &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (pending == null)
                throw new Exception(_localizer["Backend_TwitchPendingSessionInvalidOrExpired"]);

            string? matchedUsername = null;

            if (pending.MatchedUserAccountId.HasValue)
            {
                var matchedUser = await context.UserAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == pending.MatchedUserAccountId.Value);

                matchedUsername = matchedUser?.Username;
            }

            var suggestedUsername = pending.TwitchLogin ?? pending.TwitchDisplayName ?? string.Empty;

            return new TwitchPendingSessionDto
            {
                PendingToken = pending.PublicToken,
                FlowType = pending.FlowType,
                TwitchLogin = pending.TwitchLogin,
                TwitchDisplayName = pending.TwitchDisplayName,
                TwitchEmail = pending.TwitchEmail,
                MatchedUsername = matchedUsername,
                SuggestedUsername = suggestedUsername,
                SuggestedUsernameAvailable = await _authService.IsUsernameAvailableAsync(suggestedUsername),
                Language = pending.Language
            };
        }

        public async Task<Guid> CompleteRegistrationAsync(CompleteTwitchRegistrationRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = await context.Set<TwitchPendingAuthSession>()
                .FirstOrDefaultAsync(x =>
                    x.PublicToken == request.PendingToken &&
                    x.FlowType == "complete" &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (pending == null)
                throw new Exception(_localizer["Backend_TwitchPendingSessionInvalidOrExpired"]);

            var username = request.Username?.Trim() ?? string.Empty;
            var email = string.IsNullOrWhiteSpace(request.Email) ? pending.TwitchEmail : request.Email.Trim().ToLowerInvariant();

            var user = await _authService.CreateTwitchAccountAsync(
                username,
                email,
                request.Language,
                pending.TwitchUserId!,
                pending.TwitchLogin!,
                pending.TwitchDisplayName);

            pending.ConsumedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return user.Id;
        }

        public async Task<Guid> ConfirmMergeAsync(ConfirmTwitchMergeRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = await context.Set<TwitchPendingAuthSession>()
                .FirstOrDefaultAsync(x =>
                    x.PublicToken == request.PendingToken &&
                    x.FlowType == "merge" &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (pending == null || !pending.MatchedUserAccountId.HasValue)
                throw new Exception(_localizer["Backend_TwitchPendingSessionInvalidOrExpired"]);

            await _authService.LinkTwitchToExistingAccountAsync(
                pending.MatchedUserAccountId.Value,
                pending.TwitchUserId!,
                pending.TwitchLogin!,
                pending.TwitchDisplayName,
                pending.TwitchEmail);

            // Optionnel : aligner le username local sur le login Twitch si disponible.
            if (request.RenameUsernameToTwitchLogin && !string.IsNullOrWhiteSpace(pending.TwitchLogin))
            {
                var matchedUser = await context.UserAccounts
                    .FirstOrDefaultAsync(x => x.Id == pending.MatchedUserAccountId.Value);

                if (matchedUser != null &&
                    !string.Equals(matchedUser.Username, pending.TwitchLogin, StringComparison.Ordinal))
                {
                    var isAvailable = !await context.UserAccounts
                        .AnyAsync(x => x.Username == pending.TwitchLogin && x.Id != matchedUser.Id);

                    if (isAvailable)
                    {
                        matchedUser.Username = pending.TwitchLogin;
                        matchedUser.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                        matchedUser.LastSensitiveChangeType = "Account_SecurityHistory_UsernameChangedFromTwitch";
                    }
                }
            }

            pending.ConsumedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return pending.MatchedUserAccountId.Value;
        }

        public async Task LinkCurrentUserToTwitchAsync(Guid currentUserId, string pendingToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var pending = await context.Set<TwitchPendingAuthSession>()
                .FirstOrDefaultAsync(x =>
                    x.PublicToken == pendingToken &&
                    x.ConsumedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow);

            if (pending == null)
                throw new Exception(_localizer["Backend_TwitchPendingSessionInvalidOrExpired"]);

            await _authService.LinkTwitchToExistingAccountAsync(
                currentUserId,
                pending.TwitchUserId!,
                pending.TwitchLogin!,
                pending.TwitchDisplayName,
                pending.TwitchEmail);

            pending.ConsumedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task UnlinkTwitchAsync(Guid currentUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == currentUserId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            user.IsTwitchLinked = false;
            user.TwitchUserId = null;
            user.TwitchLogin = null;
            user.TwitchDisplayName = null;

            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_TwitchUnlinked";

            await context.SaveChangesAsync();
        }

        public async Task RenameCurrentUserToTwitchLoginAsync(Guid currentUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == currentUserId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (!user.IsTwitchLinked || string.IsNullOrWhiteSpace(user.TwitchLogin))
                throw new Exception(_localizer["Backend_TwitchNotLinked"]);

            if (string.Equals(user.Username, user.TwitchLogin, StringComparison.Ordinal))
                return;

            var existingOwner = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Username == user.TwitchLogin && x.Id != currentUserId);

            if (existingOwner != null)
                throw new Exception(_localizer["Backend_TwitchUsernameUnavailable"]);

            user.Username = user.TwitchLogin;
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_UsernameChangedFromTwitch";

            await context.SaveChangesAsync();
        }

        private string BuildAuthorizeUrl(string state)
        {
            var redirectUri = Uri.EscapeDataString(BuildCallbackUrl());
            var clientId = Uri.EscapeDataString(_options.ClientId);
            var scope = Uri.EscapeDataString("user:read:email");
            var encodedState = Uri.EscapeDataString(state);

            return $"https://id.twitch.tv/oauth2/authorize" +
                   $"?response_type=code" +
                   $"&client_id={clientId}" +
                   $"&redirect_uri={redirectUri}" +
                   $"&scope={scope}" +
                   $"&state={encodedState}";
        }

        private string BuildCallbackUrl()
        {
            var baseUrl = (_options.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            var path = (_options.CallbackPath ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception("Twitch:PublicBaseUrl is not configured.");

            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Twitch:CallbackPath is not configured.");

            return $"{baseUrl}{path}";
        }

        private async Task<TwitchUserInfo> ExchangeCodeAndGetUserAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new Exception(_localizer["Backend_TwitchAuthorizationCodeMissing"]);

            var http = _httpClientFactory.CreateClient();

            var tokenForm = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = BuildCallbackUrl()
            };

            var tokenResponse = await http.PostAsync(
                "https://id.twitch.tv/oauth2/token",
                new FormUrlEncodedContent(tokenForm));

            if (!tokenResponse.IsSuccessStatusCode)
                throw new Exception(_localizer["Backend_TwitchTokenExchangeFailed"]);

            var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TwitchTokenResponse>();

            if (tokenPayload == null || string.IsNullOrWhiteSpace(tokenPayload.access_token))
                throw new Exception(_localizer["Backend_TwitchTokenExchangeFailed"]);

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.access_token);
            userRequest.Headers.Add("Client-Id", _options.ClientId);

            var userResponse = await http.SendAsync(userRequest);

            if (!userResponse.IsSuccessStatusCode)
                throw new Exception(_localizer["Backend_TwitchUserFetchFailed"]);

            var userPayload = await userResponse.Content.ReadFromJsonAsync<TwitchUsersResponse>();

            var user = userPayload?.data?.FirstOrDefault();

            if (user == null || string.IsNullOrWhiteSpace(user.id) || string.IsNullOrWhiteSpace(user.login))
                throw new Exception(_localizer["Backend_TwitchUserFetchFailed"]);

            return new TwitchUserInfo
            {
                Id = user.id,
                Login = user.login,
                DisplayName = user.display_name,
                Email = user.email
            };
        }

        private static string CreateToken()
        {
            return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
        }

        private static string NormalizeLanguage(string language)
        {
            var normalized = language?.Trim().ToLowerInvariant();

            return normalized switch
            {
                "en" => "en",
                _ => "fr"
            };
        }

        private class TwitchTokenResponse
        {
            public string? access_token { get; set; }
        }

        private class TwitchUsersResponse
        {
            public List<TwitchUserPayload> data { get; set; } = new();
        }

        private class TwitchUserPayload
        {
            public string id { get; set; } = string.Empty;
            public string login { get; set; } = string.Empty;
            public string display_name { get; set; } = string.Empty;
            public string? email { get; set; }
        }

        private class TwitchUserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Login { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string? Email { get; set; }
        }
    }
}
