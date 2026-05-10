
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.GameRules;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace Rollocracy.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly IPresenceTracker _presenceTracker;
        private readonly ISessionNotifier _sessionNotifier;
        private readonly IAccountService _accountService;

        public SessionService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            IPresenceTracker presenceTracker,
            ISessionNotifier sessionNotifier,
            IAccountService accountService)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _presenceTracker = presenceTracker;
            _sessionNotifier = sessionNotifier;
            _accountService = accountService;
        }

        public async Task<Session> CreateSessionAsync(
            Guid gameMasterUserAccountId,
            Guid gameSystemId,
            string sessionName,
            string sessionPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await _accountService.ApplyDueSubscriptionChangesAsync(gameMasterUserAccountId);

            var gameMasterUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == gameMasterUserAccountId);

            if (gameMasterUser == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedJms = NormalizeSessionCapacity(gameMasterUser.MaxPlayersPerSession);

            var trimmedSessionName = sessionName.Trim();

            if (string.IsNullOrWhiteSpace(trimmedSessionName))
                throw new Exception(_localizer["Backend_SessionNameRequired"]);

            if (trimmedSessionName.Length > 16)
                throw new Exception(_localizer["Backend_SessionNameTooLong"]);

            if (normalizedJms <= 0)
                throw new Exception(_localizer["Backend_OnlyUsersWithPositiveJmsCanCreateSession"]);

            var selectedGameSystem = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.LockedToSessionId == null &&
                    (gs.OwnerUserAccountId == gameMasterUserAccountId || gs.IsGeneric));

            if (selectedGameSystem == null)
                throw new Exception(_localizer["Backend_SourceGameSystemNotFound"]);

            var sessionSlug = GenerateSessionSlug(trimmedSessionName);

            var existingSession = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.GameMasterUserAccountId == gameMasterUserAccountId &&
                    s.SessionSlug == sessionSlug);

            if (existingSession != null)
                throw new Exception(_localizer["Backend_SessionNameAlreadyExists"]);

            var session = new Session
            {
                Id = Guid.NewGuid(),
                GameMasterUserAccountId = gameMasterUserAccountId,
                GameSystemId = selectedGameSystem.Id,
                SessionName = trimmedSessionName,
                SessionSlug = sessionSlug,
                SessionPassword = sessionPassword.Trim(),
                IsActive = false
            };

            context.Sessions.Add(session);

            var gmPlayerSession = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserAccountId = gameMasterUser.Id,
                PlayerName = gameMasterUser.Username,
                IsGameMaster = true,
                SpecialRole = SessionSpecialRole.None
            };

            context.PlayerSessions.Add(gmPlayerSession);

            await context.SaveChangesAsync();

            return session;
        }

        public async Task<PlayerSession> JoinSessionAsync(
            string gameMasterUsername,
            string sessionSlug,
            string sessionPassword,
            Guid userAccountId,
            string playerName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = gameMasterUsername.Trim().ToLower();
            var normalizedSlug = sessionSlug.Trim().ToLower();

            var gameMasterUser = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

            if (gameMasterUser == null)
                throw new Exception(_localizer["Backend_GameMasterNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.GameMasterUserAccountId == gameMasterUser.Id &&
                    s.SessionSlug.ToLower() == normalizedSlug);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            if (!session.IsActive)
                throw new Exception(_localizer["Session_Inactive"]);

            if (!string.IsNullOrWhiteSpace(session.SessionPassword) && session.SessionPassword != sessionPassword)
                throw new Exception(_localizer["Backend_InvalidSessionPassword"]);

            var existingPlayerSession = await context.PlayerSessions
                .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserAccountId == userAccountId);

            var existingPlayerHasAliveCharacter = false;
            if (existingPlayerSession != null)
            {
                existingPlayerHasAliveCharacter = await context.Characters
                    .AsNoTracking()
                    .AnyAsync(c => c.PlayerSessionId == existingPlayerSession.Id && c.IsAlive);
            }

            var sessionCapacity = NormalizeSessionCapacity(gameMasterUser.MaxPlayersPerSession);
            if (sessionCapacity <= 0)
                throw new Exception(_localizer["Backend_SessionIsFull"]);

            var onlineLivingPlayersExcludingCurrent = await GetOnlineLivingPlayersCountAsync(
                context,
                session.Id,
                existingPlayerSession?.Id);

            var currentUserWouldConsumeASlot = existingPlayerSession == null || existingPlayerHasAliveCharacter;
            if (currentUserWouldConsumeASlot && onlineLivingPlayersExcludingCurrent >= sessionCapacity)
                throw new Exception(_localizer["Backend_SessionIsFull"]);

            if (existingPlayerSession != null)
                return existingPlayerSession;

            var playerSession = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserAccountId = userAccountId,
                PlayerName = playerName,
                IsGameMaster = false,
                SpecialRole = SessionSpecialRole.None
            };

            context.PlayerSessions.Add(playerSession);
            await context.SaveChangesAsync();

            return playerSession;
        }

        public async Task<List<PlayerSession>> GetPlayersAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.PlayerSessions
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.JoinedAt)
                .ToListAsync();
        }

        public async Task<List<PlayerSession>> GetEligibleSpecialRolePlayersAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            var playerSessions = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId && !ps.IsGameMaster)
                .OrderBy(ps => ps.PlayerName)
                .ToListAsync();

            var playerSessionIdsWithCharacters = await context.Characters
                .AsNoTracking()
                .Join(context.PlayerSessions.AsNoTracking(), c => c.PlayerSessionId, ps => ps.Id, (c, ps) => new { c, ps })
                .Where(x => x.ps.SessionId == sessionId)
                .Select(x => x.c.PlayerSessionId)
                .Distinct()
                .ToListAsync();

            return playerSessions
                .Where(ps => playerSessionIdsWithCharacters.Contains(ps.Id))
                .ToList();
        }

        public async Task<List<PlayerSession>> GetPlayersWithSpecialRolesAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            return await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId && !ps.IsGameMaster && ps.SpecialRole != SessionSpecialRole.None)
                .OrderBy(ps => ps.PlayerName)
                .ToListAsync();
        }

        public async Task AssignSpecialRoleAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid playerSessionId, SessionSpecialRole role)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            if (role == SessionSpecialRole.None)
                throw new Exception(_localizer["Backend_InvalidSpecialRole"]);

            var playerSession = await context.PlayerSessions
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId && ps.SessionId == sessionId);

            if (playerSession == null || playerSession.IsGameMaster)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var hasCharacter = await context.Characters
                .AsNoTracking()
                .AnyAsync(c => c.PlayerSessionId == playerSessionId);

            if (!hasCharacter)
                throw new Exception(_localizer["Backend_SpecialRoleRequiresCharacter"]);

            playerSession.SpecialRole = role;
            await context.SaveChangesAsync();
        }

        public async Task RemoveSpecialRoleAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            var playerSession = await context.PlayerSessions
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId && ps.SessionId == sessionId);

            if (playerSession == null || playerSession.IsGameMaster)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            playerSession.SpecialRole = SessionSpecialRole.None;
            await context.SaveChangesAsync();
        }

        private async Task<(bool CanView, bool CanEdit)> GetSessionGaugePermissionsAsync(
            RollocracyDbContext context,
            Guid sessionId,
            Guid userAccountId)
        {
            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return (false, false);

            if (session.GameMasterUserAccountId == userAccountId)
                return (true, true);

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.SessionId == sessionId && ps.UserAccountId == userAccountId);

            if (playerSession == null)
                return (false, false);

            return playerSession.SpecialRole switch
            {
                SessionSpecialRole.Assistant => (true, true),
                SessionSpecialRole.Observer => (true, false),
                _ => (false, false)
            };
        }

        private static int ClampSessionGaugeValue(SessionGauge gauge, int currentValue)
        {
            return Math.Clamp(currentValue, gauge.MinValue, gauge.MaxValue);
        }

        public async Task<Session?> GetSessionByOwnerAndSlugAsync(string gameMasterUsername, string sessionSlug)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedUsername = gameMasterUsername.Trim().ToLower();
            var normalizedSlug = sessionSlug.Trim().ToLower();

            var gameMasterUser = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

            if (gameMasterUser == null)
                return null;

            return await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.GameMasterUserAccountId == gameMasterUser.Id &&
                    s.SessionSlug.ToLower() == normalizedSlug);
        }

        public async Task<Session?> GetSessionByIdAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<PlayerSession?> GetPlayerByIdAsync(Guid playerId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        public async Task<List<Session>> GetSessionsByGameMasterAsync(Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Sessions
                .AsNoTracking()
                .Where(s => s.GameMasterUserAccountId == gameMasterUserAccountId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PlayerResumableSessionDto>> GetResumablePlayerSessionsAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSessions = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.UserAccountId == userAccountId && !ps.IsGameMaster)
                .ToListAsync();

            if (playerSessions.Count == 0)
                return new List<PlayerResumableSessionDto>();

            var playerSessionIds = playerSessions.Select(x => x.Id).ToList();
            var sessionIds = playerSessions.Select(x => x.SessionId).Distinct().ToList();

            var sessions = await context.Sessions
                .AsNoTracking()
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();

            var gameMasterIds = sessions.Select(x => x.GameMasterUserAccountId).Distinct().ToList();

            var gameMasters = await context.UserAccounts
                .AsNoTracking()
                .Where(u => gameMasterIds.Contains(u.Id))
                .ToListAsync();

            var characters = await context.Characters
                .AsNoTracking()
                .Where(c => playerSessionIds.Contains(c.PlayerSessionId))
                .ToListAsync();

            var result = new List<PlayerResumableSessionDto>();

            foreach (var playerSession in playerSessions)
            {
                var session = sessions.FirstOrDefault(x => x.Id == playerSession.SessionId);
                if (session == null)
                    continue;

                var gm = gameMasters.FirstOrDefault(x => x.Id == session.GameMasterUserAccountId);
                var playerCharacters = characters.Where(c => c.PlayerSessionId == playerSession.Id).ToList();

                var aliveCount = playerCharacters.Count(c => c.IsAlive);
                var deadCount = playerCharacters.Count(c => !c.IsAlive);
                var totalCount = playerCharacters.Count;

                if (totalCount == 0)
                    continue;

                result.Add(new PlayerResumableSessionDto
                {
                    PlayerSessionId = playerSession.Id,
                    SessionId = session.Id,
                    SessionName = session.SessionName,
                    SessionSlug = session.SessionSlug,
                    GameMasterUsername = gm?.Username ?? string.Empty,
                    SessionIsActive = session.IsActive,
                    TotalCharactersCount = totalCount,
                    AliveCharactersCount = aliveCount,
                    DeadCharactersCount = deadCount,
                    SpecialRole = playerSession.SpecialRole
                });
            }

            return result
                .OrderByDescending(x => x.AliveCharactersCount)
                .ThenBy(x => x.GameMasterUsername)
                .ThenBy(x => x.SessionName)
                .ToList();
        }

        public async Task<Guid> JoinSessionAsync(
    Guid userAccountId,
    string gameMasterUsername,
    string sessionSlug,
    string? sessionPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(gameMasterUsername))
                throw new Exception(_localizer["Backend_JoinSessionGameMasterRequired"]);

            if (string.IsNullOrWhiteSpace(sessionSlug))
                throw new Exception(_localizer["Backend_JoinSessionSlugRequired"]);

            var normalizedGameMasterUsername = gameMasterUsername.Trim();
            var normalizedSessionSlug = sessionSlug.Trim();

            var userAccount = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (userAccount == null)
                throw new Exception(_localizer["Backend_UserNotFound"]);

            var gameMaster = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedGameMasterUsername.ToLower());

            if (gameMaster == null)
                throw new Exception(_localizer["Backend_JoinSessionNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.GameMasterUserAccountId == gameMaster.Id &&
                    s.SessionSlug.ToLower() == normalizedSessionSlug.ToLower());

            if (session == null)
                throw new Exception(_localizer["Backend_JoinSessionNotFound"]);

            if (!session.IsActive)
                throw new Exception(_localizer["Session_Inactive"]);

            var expectedPassword = session.SessionPassword?.Trim() ?? string.Empty;
            var providedPassword = sessionPassword?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(expectedPassword) && !string.Equals(expectedPassword, providedPassword, StringComparison.Ordinal))
                throw new Exception(_localizer["Backend_JoinSessionInvalidPassword"]);

            var existingPlayerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps =>
                    ps.SessionId == session.Id &&
                    ps.UserAccountId == userAccountId &&
                    !ps.IsGameMaster);

            if (existingPlayerSession != null)
                return existingPlayerSession.Id;

            var playerSession = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserAccountId = userAccountId,
                PlayerName = userAccount.Username,
                IsGameMaster = false,
                SpecialRole = SessionSpecialRole.None,
                JoinedAt = DateTime.UtcNow
            };

            context.PlayerSessions.Add(playerSession);
            await context.SaveChangesAsync();

            return playerSession.Id;
        }

        public async Task SetSessionActiveStateAsync(Guid sessionId, Guid gameMasterUserAccountId, bool isActive)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            session.IsActive = isActive;
            await context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            if (session.IsActive)
                throw new Exception(_localizer["Backend_CannotDeleteActiveSession"]);

            // Système dupliqué et verrouillé à cette session, s'il existe.
            var lockedGameSystem = await context.GameSystems
                .FirstOrDefaultAsync(gs =>
                    gs.OwnerUserAccountId == gameMasterUserAccountId &&
                    gs.LockedToSessionId == sessionId);

            // Si la session pointe encore vers ce système dupliqué,
            // on détache d'abord la FK pour éviter tout conflit de suppression.
            if (lockedGameSystem is not null && session.GameSystemId == lockedGameSystem.Id)
            {
                session.GameSystemId = null;
                await context.SaveChangesAsync();
            }

            // Suppression du système dédié à la session, puis de la session.
            // Les dépendances exclusives sont censées suivre la cascade existante.
            if (lockedGameSystem is not null)
            {
                context.GameSystems.Remove(lockedGameSystem);
            }

            context.Sessions.Remove(session);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<int> GetAliveCharacterCountAsync(Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Characters
                .AsNoTracking()
                .Where(c => c.IsAlive)
                .Join(context.PlayerSessions, character => character.PlayerSessionId, playerSession => playerSession.Id, (character, playerSession) => new { character, playerSession })
                .Where(x => x.playerSession.SessionId == sessionId)
                .CountAsync();
        }

        public async Task AssignGameSystemToSessionAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid gameSystemId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.LockedToSessionId == null &&
                    (gs.OwnerUserAccountId == gameMasterUserAccountId || gs.IsGeneric));

            if (system == null)
                throw new Exception(_localizer["GameSystem_NotFound"]);

            session.GameSystemId = gameSystemId;
            await context.SaveChangesAsync();
        }

        public async Task<bool> CanUserCreateSessionsAsync(Guid userAccountId)
        {
            await _accountService.ApplyDueSubscriptionChangesAsync(userAccountId);
            var maxPlayers = await GetUserMaxPlayersPerSessionAsync(userAccountId);
            return maxPlayers > 0;
        }

        public async Task<int> GetUserMaxPlayersPerSessionAsync(Guid userAccountId)
        {
            await _accountService.ApplyDueSubscriptionChangesAsync(userAccountId);
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            return NormalizeSessionCapacity(user.MaxPlayersPerSession);
        }

        public async Task<SessionSettingsDto?> GetSessionSettingsAsync(Guid sessionId, Guid gameMasterUserAccountId, string baseUri)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                return null;

            var gameMasterUser = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == gameMasterUserAccountId);

            if (gameMasterUser == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedBaseUri = baseUri.EndsWith("/") ? baseUri : $"{baseUri}/";

            return new SessionSettingsDto
            {
                SessionId = session.Id,
                GameSystemId = session.GameSystemId,
                SessionName = session.SessionName,
                SessionSlug = session.SessionSlug,
                SessionPassword = session.SessionPassword,
                IsActive = session.IsActive,
                JoinUrl = $"{normalizedBaseUri}{gameMasterUser.Username}/{session.SessionSlug}"
            };
        }

        public async Task<Session> UpdateSessionSettingsAsync(Guid sessionId, Guid gameMasterUserAccountId, string sessionName, string sessionPassword, bool updateJoinUrlSlug)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var trimmedSessionName = sessionName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSessionName))
                throw new Exception(_localizer["Backend_SessionNameRequired"]);

            if (trimmedSessionName.Length > 16)
                throw new Exception(_localizer["Backend_SessionNameTooLong"]);

            session.SessionName = trimmedSessionName;
            session.SessionPassword = sessionPassword.Trim();

            if (updateJoinUrlSlug)
            {
                var newSlug = GenerateSessionSlug(trimmedSessionName);

                var slugAlreadyExists = await context.Sessions
                    .AsNoTracking()
                    .AnyAsync(s => s.Id != sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId && s.SessionSlug == newSlug);

                if (slugAlreadyExists)
                    throw new Exception(_localizer["Backend_SessionNameAlreadyExists"]);

                session.SessionSlug = newSlug;
            }

            await context.SaveChangesAsync();
            return session;
        }

        public async Task<List<SessionGaugeDto>> GetSessionGaugesAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            return await context.SessionGauges
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new SessionGaugeDto
                {
                    SessionGaugeId = x.Id,
                    Name = x.Name,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    CurrentValue = x.CurrentValue
                })
                .ToListAsync();
        }

        public async Task<List<SessionGaugeDto>> GetVisibleSessionGaugesAsync(Guid sessionId, Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var permissions = await GetSessionGaugePermissionsAsync(context, sessionId, userAccountId);

            if (!permissions.CanView)
                return new List<SessionGaugeDto>();

            return await context.SessionGauges
                .AsNoTracking()
                .Where(g => g.SessionId == sessionId)
                .OrderBy(g => g.Name)
                .Select(g => new SessionGaugeDto
                {
                    SessionGaugeId = g.Id,
                    Name = g.Name,
                    MinValue = g.MinValue,
                    MaxValue = g.MaxValue,
                    CurrentValue = g.CurrentValue
                })
                .ToListAsync();
        }

        public async Task UpdateSessionGaugeCurrentValueAsync(
            Guid sessionId,
            Guid userAccountId,
            Guid sessionGaugeId,
            int currentValue)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var permissions = await GetSessionGaugePermissionsAsync(context, sessionId, userAccountId);

            if (!permissions.CanEdit)
                throw new Exception(_localizer["Backend_SessionGaugeEditAccessDenied"]);

            var gauge = await context.SessionGauges
                .FirstOrDefaultAsync(g => g.Id == sessionGaugeId && g.SessionId == sessionId);

            if (gauge == null)
                throw new Exception(_localizer["Backend_SessionGaugeNotFound"]);

            gauge.CurrentValue = ClampSessionGaugeValue(gauge, currentValue);

            await context.SaveChangesAsync();
            await _sessionNotifier.NotifyCharacterStateChangedAsync(sessionId);
        }

        public async Task<SessionGauge> CreateSessionGaugeAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int currentValue)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            var trimmedName = name.Trim();

            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new Exception(_localizer["Backend_SessionGaugeNameRequired"]);

            if (maxValue < minValue)
                throw new Exception(_localizer["Backend_SessionGaugeRangeInvalid"]);

            var existingCount = await context.SessionGauges
                .AsNoTracking()
                .CountAsync(x => x.SessionId == sessionId);

            if (existingCount >= 12)
                throw new Exception(_localizer["Backend_SessionGaugeLimitReached"]);

            var entity = new SessionGauge
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Name = trimmedName,
                MinValue = minValue,
                MaxValue = maxValue,
                CurrentValue = Math.Clamp(currentValue, minValue, maxValue),
                CreatedAtUtc = DateTime.UtcNow
            };

            context.SessionGauges.Add(entity);
            await context.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateSessionGaugeAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            Guid sessionGaugeId,
            string name,
            int minValue,
            int maxValue,
            int currentValue)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            var entity = await context.SessionGauges
                .FirstOrDefaultAsync(x => x.Id == sessionGaugeId && x.SessionId == sessionId);

            if (entity == null)
                throw new Exception(_localizer["Backend_SessionGaugeNotFound"]);

            var trimmedName = name.Trim();

            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new Exception(_localizer["Backend_SessionGaugeNameRequired"]);

            if (maxValue < minValue)
                throw new Exception(_localizer["Backend_SessionGaugeRangeInvalid"]);

            entity.Name = trimmedName;
            entity.MinValue = minValue;
            entity.MaxValue = maxValue;
            entity.CurrentValue = Math.Clamp(currentValue, minValue, maxValue);

            await context.SaveChangesAsync();
        }

        public async Task DeleteSessionGaugeAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid sessionGaugeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            var entity = await context.SessionGauges
                .FirstOrDefaultAsync(x => x.Id == sessionGaugeId && x.SessionId == sessionId);

            if (entity == null)
                throw new Exception(_localizer["Backend_SessionGaugeNotFound"]);

            context.SessionGauges.Remove(entity);
            await context.SaveChangesAsync();
        }

        private async Task<int> GetOnlineLivingPlayersCountAsync(RollocracyDbContext context, Guid sessionId, Guid? excludedPlayerSessionId)
        {
            var nonGameMasterPlayerSessions = await context.PlayerSessions
                .AsNoTracking()
                .Where(ps => ps.SessionId == sessionId && !ps.IsGameMaster)
                .ToListAsync();

            var onlinePlayerSessionIds = nonGameMasterPlayerSessions
                .Where(ps => (!excludedPlayerSessionId.HasValue || ps.Id != excludedPlayerSessionId.Value) && _presenceTracker.IsPlayerOnline(ps.Id))
                .Select(ps => ps.Id)
                .ToList();

            if (onlinePlayerSessionIds.Count == 0)
                return 0;

            return await context.Characters
                .AsNoTracking()
                .Where(c => c.IsAlive && onlinePlayerSessionIds.Contains(c.PlayerSessionId))
                .Select(c => c.PlayerSessionId)
                .Distinct()
                .CountAsync();
        }

        private async Task EnsureGameMasterOwnsSessionAsync(RollocracyDbContext context, Guid sessionId, Guid gameMasterUserAccountId)
        {
            var exists = await context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (!exists)
                throw new Exception(_localizer["Session_NotFound"]);
        }

        public async Task<List<EditableItemDefinitionDto>> GetSessionItemDefinitionsAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var items = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var itemIds = items.Select(x => x.Id).ToList();

            var modifiers = await context.ItemModifierDefinitions
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemDefinitionId))
                .ToListAsync();

            return items.Select(item => new EditableItemDefinitionDto
            {
                ItemDefinitionId = item.Id,
                Name = item.Name,
                Description = item.Description ?? string.Empty,
                DisplayOrder = item.DisplayOrder,
                IsDeleted = false,
                IsConsumable = item.IsConsumable,
                MaxQuantityPerCharacter = item.MaxQuantityPerCharacter,
                Modifiers = modifiers
                    .Where(x => x.ItemDefinitionId == item.Id)
                    .Select(x => new EditableModifierDefinitionDto
                    {
                        Id = x.Id,
                        OperationType = ModifierOperationType.Grant,
                        TargetType = x.TargetType,
                        TargetId = x.TargetId,
                        Value = x.AddValue,
                        FillGaugeCurrentValueOnly = x.FillGaugeCurrentValueOnly,
                        ValueMode = x.ValueMode,
                        SourceMetricId = x.SourceMetricId
                    })
                    .ToList()
            }).ToList();
        }

        public async Task SaveSessionItemDefinitionsAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            List<EditableItemDefinitionDto> requestItems)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            if (!session.GameSystemId.HasValue)
                throw new Exception(_localizer["Backend_SessionHasNoGameSystem"]);

            var gameSystemId = session.GameSystemId.Value;

            var affectedCharacterIds = await context.Characters
                .Join(context.PlayerSessions,
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .Where(x => x.playerSession.SessionId == sessionId)
                .Select(x => x.character.Id)
                .ToListAsync();

            foreach (var item in requestItems.Where(x => !x.IsDeleted))
            {
                if (item.IsConsumable && item.MaxQuantityPerCharacter <= 0)
                    throw new Exception(_localizer["Backend_ConsumableItemMaxQuantityInvalid"]);
            }

            await ValidateSessionItemNamesAsync(context, sessionId, gameSystemId, requestItems);
            await SyncSessionItemsAsync(context, sessionId, affectedCharacterIds, requestItems);
            await SyncSessionItemModifiersAsync(context, requestItems);

            await context.SaveChangesAsync();
            await _sessionNotifier.NotifyCharacterStateChangedAsync(sessionId);
        }

        public async Task<SessionStoreEditorDto?> GetSessionStoreEditorAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                return null;

            var availableGaugeDefinitions = session.GameSystemId.HasValue
                ? await context.GaugeDefinitions
                    .AsNoTracking()
                    .Where(x => x.GameSystemId == session.GameSystemId.Value)
                    .OrderBy(x => x.Name)
                    .ToListAsync()
                : new List<GaugeDefinition>();

            var store = await context.SessionStores
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);

            var offers = new List<SessionStoreOffer>();

            if (store is not null)
            {
                offers = await context.SessionStoreOffers
                    .AsNoTracking()
                    .Where(x => x.SessionStoreId == store.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();
            }

            var availableTalents = new List<SessionStoreReferenceOptionDto>();
            if (session.GameSystemId.HasValue)
            {
                availableTalents = await context.TalentDefinitions
                    .AsNoTracking()
                    .Where(x => x.GameSystemId == session.GameSystemId.Value)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new SessionStoreReferenceOptionDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IsConsumable = false,
                        MaxQuantityPerCharacter = 1
                    })
                    .ToListAsync();
            }

            var availableItems = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x =>
                    (session.GameSystemId.HasValue && x.GameSystemId == session.GameSystemId.Value) ||
                    x.SessionId == sessionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new SessionStoreReferenceOptionDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsConsumable = x.IsConsumable,
                    MaxQuantityPerCharacter = x.MaxQuantityPerCharacter
                })
                .ToListAsync();

            return new SessionStoreEditorDto
            {
                SessionId = sessionId,
                IsEnabled = store?.IsEnabled ?? false,
                AvailableGauges = availableGaugeDefinitions.Select(x => new SessionStoreGaugeOptionDto
                {
                    GaugeDefinitionId = x.Id,
                    Name = x.Name
                }).ToList(),
                AvailableTalents = availableTalents,
                AvailableItems = availableItems,
                Offers = offers.Select(x => new SessionStoreOfferEditorDto
                {
                    OfferId = x.Id,
                    OfferType = x.OfferType,
                    TargetDefinitionId = x.TargetDefinitionId,
                    CurrencyGaugeDefinitionId = x.CurrencyGaugeDefinitionId,
                    Cost = x.Cost,
                    DisplayOrder = x.DisplayOrder
                }).ToList()
            };
        }

        public async Task SaveSessionStoreAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            SessionStoreEditorDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sessionId && x.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var validGaugeDefinitionIds = session.GameSystemId.HasValue
                ? (await context.GaugeDefinitions
                    .AsNoTracking()
                    .Where(x => x.GameSystemId == session.GameSystemId.Value)
                    .Select(x => x.Id)
                    .ToListAsync()).ToHashSet()
                : new HashSet<Guid>();

            var validTalentIds = session.GameSystemId.HasValue
                ? await context.TalentDefinitions
                    .AsNoTracking()
                    .Where(x => x.GameSystemId == session.GameSystemId.Value)
                    .Select(x => x.Id)
                    .ToListAsync()
                : new List<Guid>();

            var validItemIds = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x =>
                    (session.GameSystemId.HasValue && x.GameSystemId == session.GameSystemId.Value) ||
                    x.SessionId == sessionId)
                .Select(x => x.Id)
                .ToListAsync();

            var validTalentIdSet = validTalentIds.ToHashSet();
            var validItemIdSet = validItemIds.ToHashSet();

            var incomingActiveOffers = request.Offers
                .Where(x => !x.IsDeleted)
                .ToList();

            var activeOffers = incomingActiveOffers
                .Where(offer => offer.OfferType switch
                {
                    SessionStoreOfferType.Talent => validTalentIdSet.Contains(offer.TargetDefinitionId),
                    SessionStoreOfferType.Item => validItemIdSet.Contains(offer.TargetDefinitionId),
                    _ => false
                })
                .ToList();

            foreach (var offer in activeOffers)
            {
                if (!validGaugeDefinitionIds.Contains(offer.CurrencyGaugeDefinitionId))
                    throw new Exception(_localizer["Backend_SessionStoreInvalidGauge"]);

                if (offer.Cost < 0)
                    throw new Exception(_localizer["Backend_SessionStoreInvalidCost"]);
            }

            var store = await context.SessionStores
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);

            if (store is null)
            {
                store = new SessionStore
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    IsEnabled = request.IsEnabled
                };

                context.SessionStores.Add(store);
                await context.SaveChangesAsync();
            }
            else
            {
                store.IsEnabled = request.IsEnabled;
            }

            var existingOffers = await context.SessionStoreOffers
                .Where(x => x.SessionStoreId == store.Id)
                .ToListAsync();

            var requestIds = activeOffers
                .Where(x => x.OfferId.HasValue)
                .Select(x => x.OfferId!.Value)
                .ToHashSet();

            var toRemove = existingOffers
                .Where(x => !requestIds.Contains(x.Id))
                .ToList();

            if (toRemove.Count > 0)
            {
                context.SessionStoreOffers.RemoveRange(toRemove);
            }

            foreach (var offer in activeOffers.Where(x => x.OfferId.HasValue))
            {
                var entity = existingOffers.First(x => x.Id == offer.OfferId!.Value);
                entity.OfferType = offer.OfferType;
                entity.TargetDefinitionId = offer.TargetDefinitionId;
                entity.CurrencyGaugeDefinitionId = offer.CurrencyGaugeDefinitionId;
                entity.Cost = offer.Cost;
                entity.DisplayOrder = offer.DisplayOrder;
            }

            foreach (var offer in activeOffers.Where(x => !x.OfferId.HasValue))
            {
                context.SessionStoreOffers.Add(new SessionStoreOffer
                {
                    Id = Guid.NewGuid(),
                    SessionStoreId = store.Id,
                    OfferType = offer.OfferType,
                    TargetDefinitionId = offer.TargetDefinitionId,
                    CurrencyGaugeDefinitionId = offer.CurrencyGaugeDefinitionId,
                    Cost = offer.Cost,
                    DisplayOrder = offer.DisplayOrder
                });
            }

            await context.SaveChangesAsync();
            await _sessionNotifier.NotifyStoreChangedAsync(sessionId);
        }

        public async Task<PlayerSessionStoreDto> GetPlayerSessionStoreAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == playerSessionId);

            if (playerSession == null)
                throw new Exception(_localizer["Backend_PlayerSessionNotFound"]);

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == playerSession.SessionId);

            if (session == null)
                throw new Exception(_localizer["Session_NotFound"]);

            var store = await context.SessionStores
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SessionId == session.Id);

            if (store is null)
            {
                return new PlayerSessionStoreDto
                {
                    Exists = false,
                    IsEnabled = false
                };
            }

            var offers = await context.SessionStoreOffers
                .AsNoTracking()
                .Where(x => x.SessionStoreId == store.Id)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var gaugeDefinitions = session.GameSystemId.HasValue
                ? await context.GaugeDefinitions
                    .AsNoTracking()
                    .Where(x => x.GameSystemId == session.GameSystemId.Value)
                    .ToListAsync()
                : new List<GaugeDefinition>();

            var aliveCharacter = await context.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PlayerSessionId == playerSessionId && x.IsAlive);

            var characterGaugeValues = aliveCharacter is null
                ? new List<CharacterGaugeValue>()
                : await context.CharacterGaugeValues
                    .AsNoTracking()
                    .Where(x => x.CharacterId == aliveCharacter.Id)
                    .ToListAsync();

            var talentIds = offers.Where(x => x.OfferType == SessionStoreOfferType.Talent).Select(x => x.TargetDefinitionId).Distinct().ToList();
            var itemIds = offers.Where(x => x.OfferType == SessionStoreOfferType.Item).Select(x => x.TargetDefinitionId).Distinct().ToList();

            var talents = await context.TalentDefinitions
                .AsNoTracking()
                .Where(x => talentIds.Contains(x.Id))
                .ToListAsync();

            var items = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.Id))
                .ToListAsync();

            var ownedTalentIds = aliveCharacter is null
                ? new HashSet<Guid>()
                : (await context.CharacterTalents
                    .AsNoTracking()
                    .Where(x => x.CharacterId == aliveCharacter.Id)
                    .Select(x => x.TalentDefinitionId)
                    .ToListAsync()).ToHashSet();

            var ownedItems = aliveCharacter is null
                ? new List<CharacterItem>()
                : await context.CharacterItems
                    .AsNoTracking()
                    .Where(x => x.CharacterId == aliveCharacter.Id)
                    .ToListAsync();

            return new PlayerSessionStoreDto
            {
                Exists = true,
                IsEnabled = store.IsEnabled,
                AvailableGauges = gaugeDefinitions
                    .Where(g => offers.Any(o => o.CurrencyGaugeDefinitionId == g.Id))
                    .OrderBy(g => g.Name)
                    .Select(g => new SessionStoreGaugeOptionDto
                    {
                        GaugeDefinitionId = g.Id,
                        Name = g.Name
                    })
                    .ToList(),
                Offers = offers.Select(offer =>
                {
                    var gaugeDefinition = gaugeDefinitions.First(x => x.Id == offer.CurrencyGaugeDefinitionId);
                    var characterGaugeValue = aliveCharacter is null
                        ? null
                        : characterGaugeValues.FirstOrDefault(x => x.GaugeDefinitionId == gaugeDefinition.Id);
                    var hasEnoughCurrency = characterGaugeValue is not null && characterGaugeValue.Value >= offer.Cost;

                    if (offer.OfferType == SessionStoreOfferType.Talent)
                    {
                        var talent = talents.First(x => x.Id == offer.TargetDefinitionId);
                        var owned = aliveCharacter is not null && ownedTalentIds.Contains(talent.Id);

                        return new PlayerSessionStoreOfferDto
                        {
                            OfferId = offer.Id,
                            OfferType = offer.OfferType,
                            TargetDefinitionId = talent.Id,
                            TargetName = talent.Name,
                            CurrencyGaugeName = gaugeDefinition.Name,
                            CurrencyGaugeDefinitionId = gaugeDefinition.Id,
                            Cost = offer.Cost,
                            IsOwned = owned,
                            IsPurchasable = store.IsEnabled && aliveCharacter is not null && !owned && hasEnoughCurrency,
                            IsConsumable = false,
                            CurrentQuantity = 0,
                            MaxQuantityPerCharacter = 1
                        };
                    }

                    var item = items.First(x => x.Id == offer.TargetDefinitionId);
                    var ownedItem = aliveCharacter is null
                        ? null
                        : ownedItems.FirstOrDefault(x => x.ItemDefinitionId == item.Id);

                    var ownedQuantity = ownedItem?.Quantity ?? 0;
                    var itemOwned = item.IsConsumable ? ownedQuantity > 0 : ownedItem is not null;

                    var canBuy = store.IsEnabled &&
                                 aliveCharacter is not null &&
                                 hasEnoughCurrency &&
                                 (item.IsConsumable
                                    ? ownedQuantity < item.MaxQuantityPerCharacter
                                    : ownedItem is null);

                    return new PlayerSessionStoreOfferDto
                    {
                        OfferId = offer.Id,
                        OfferType = offer.OfferType,
                        TargetDefinitionId = item.Id,
                        TargetName = item.Name,
                        CurrencyGaugeName = gaugeDefinition.Name,
                        CurrencyGaugeDefinitionId = gaugeDefinition.Id,
                        Cost = offer.Cost,
                        IsOwned = itemOwned,
                        IsPurchasable = canBuy,
                        IsConsumable = item.IsConsumable,
                        CurrentQuantity = ownedQuantity,
                        MaxQuantityPerCharacter = item.MaxQuantityPerCharacter
                    };
                }).ToList()
            };
        }

        private async Task ValidateSessionItemNamesAsync(
            RollocracyDbContext context,
            Guid sessionId,
            Guid gameSystemId,
            List<EditableItemDefinitionDto> requestItems)
        {
            var activeNames = requestItems
                .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => x.Name.Trim().ToLowerInvariant())
                .ToList();

            if (activeNames.Count != activeNames.Distinct().Count())
                throw new Exception(_localizer["Backend_SessionItemNameAlreadyExists"]);

            var systemItemNames = await context.ItemDefinitions
                .AsNoTracking()
                .Where(x => x.GameSystemId == gameSystemId)
                .Select(x => x.Name.ToLower())
                .ToListAsync();

            if (activeNames.Any(name => systemItemNames.Contains(name)))
                throw new Exception(_localizer["Backend_SessionItemNameConflictsWithSystemItem"]);
        }

        private async Task SyncSessionItemsAsync(
            RollocracyDbContext context,
            Guid sessionId,
            List<Guid> affectedCharacterIds,
            List<EditableItemDefinitionDto> requestItems)
        {
            var currentItems = await context.ItemDefinitions
                .Where(x => x.SessionId == sessionId)
                .ToListAsync();

            var requestExistingIds = requestItems
                .Where(x => x.ItemDefinitionId.HasValue)
                .Select(x => x.ItemDefinitionId!.Value)
                .ToHashSet();

            var removedIds = requestItems
                .Where(x => x.ItemDefinitionId.HasValue && x.IsDeleted)
                .Select(x => x.ItemDefinitionId!.Value)
                .Union(currentItems.Where(x => !requestExistingIds.Contains(x.Id)).Select(x => x.Id))
                .Distinct()
                .ToList();

            if (removedIds.Count > 0)
            {
                var characterItems = await context.CharacterItems
                    .Where(x => affectedCharacterIds.Contains(x.CharacterId) && removedIds.Contains(x.ItemDefinitionId))
                    .ToListAsync();

                var modifiers = await context.ItemModifierDefinitions
                    .Where(x => removedIds.Contains(x.ItemDefinitionId))
                    .ToListAsync();

                context.CharacterItems.RemoveRange(characterItems);
                context.ItemModifierDefinitions.RemoveRange(modifiers);
                context.ItemDefinitions.RemoveRange(currentItems.Where(x => removedIds.Contains(x.Id)));
            }

            foreach (var item in requestItems.Where(x => x.ItemDefinitionId.HasValue && !x.IsDeleted))
            {
                var entity = currentItems.First(x => x.Id == item.ItemDefinitionId!.Value);
                entity.Name = item.Name.Trim();
                entity.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
                entity.DisplayOrder = item.DisplayOrder;
                entity.IsConsumable = item.IsConsumable;
                entity.MaxQuantityPerCharacter = item.IsConsumable
                    ? Math.Max(1, item.MaxQuantityPerCharacter)
                    : 1;
            }

            foreach (var item in requestItems.Where(x => !x.ItemDefinitionId.HasValue && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Name)))
            {
                var itemId = Guid.NewGuid();

                context.ItemDefinitions.Add(new ItemDefinition
                {
                    Id = itemId,
                    GameSystemId = null,
                    SessionId = sessionId,
                    Name = item.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                    IsConsumable = item.IsConsumable,
                    MaxQuantityPerCharacter = item.IsConsumable
                        ? Math.Max(1, item.MaxQuantityPerCharacter)
                        : 1,
                    DisplayOrder = item.DisplayOrder
                });

                item.ItemDefinitionId = itemId;
            }
        }

        private async Task SyncSessionItemModifiersAsync(
            RollocracyDbContext context,
            List<EditableItemDefinitionDto> requestItems)
        {
            foreach (var item in requestItems)
            {
                if (!item.ItemDefinitionId.HasValue)
                    continue;

                var currentModifiers = await context.ItemModifierDefinitions
                    .Where(x => x.ItemDefinitionId == item.ItemDefinitionId.Value)
                    .ToListAsync();

                var incomingIds = item.Modifiers
                    .Where(x => x.Id != Guid.Empty)
                    .Select(x => x.Id)
                    .ToHashSet();

                var toDelete = currentModifiers
                    .Where(x => !incomingIds.Contains(x.Id))
                    .ToList();

                if (toDelete.Count > 0)
                    context.ItemModifierDefinitions.RemoveRange(toDelete);

                foreach (var modifier in item.Modifiers)
                {
                    var sourceMetricId = modifier.ValueMode == ModifierValueMode.Metric
                        ? modifier.SourceMetricId
                        : null;

                    if (modifier.Id != Guid.Empty)
                    {
                        var entity = currentModifiers.First(x => x.Id == modifier.Id);
                        entity.TargetType = modifier.TargetType;
                        entity.TargetId = modifier.TargetId;
                        entity.AddValue = modifier.Value;
                        entity.FillGaugeCurrentValueOnly = modifier.FillGaugeCurrentValueOnly;
                        entity.ValueMode = modifier.ValueMode;
                        entity.SourceMetricId = sourceMetricId;
                    }
                    else
                    {
                        context.ItemModifierDefinitions.Add(new ItemModifierDefinition
                        {
                            Id = Guid.NewGuid(),
                            ItemDefinitionId = item.ItemDefinitionId.Value,
                            TargetType = modifier.TargetType,
                            TargetId = modifier.TargetId,
                            AddValue = modifier.Value,
                            FillGaugeCurrentValueOnly = modifier.FillGaugeCurrentValueOnly,
                            ValueMode = modifier.ValueMode,
                            SourceMetricId = sourceMetricId
                        });
                    }
                }
            }
        }

        public async Task<SessionJournalEditorDto?> GetSessionJournalEditorAsync(Guid sessionId, Guid gameMasterUserAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                return null;

            var hasChanges = false;

            hasChanges |= await EnsureJournalHasAtLeastOnePageAsync(context, sessionId, isPublic: false);
            hasChanges |= await EnsureJournalHasAtLeastOnePageAsync(context, sessionId, isPublic: true);

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }

            var privatePages = await context.SessionJournalPages
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId && !x.IsPublic)
                .OrderBy(x => x.PageNumber)
                .ToListAsync();

            var publicPages = await context.SessionJournalPages
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId && x.IsPublic)
                .OrderBy(x => x.PageNumber)
                .ToListAsync();

            return new SessionJournalEditorDto
            {
                SessionId = session.Id,
                SessionName = session.SessionName,
                PrivatePages = privatePages.Select(MapJournalPage).ToList(),
                PublicPages = publicPages.Select(MapJournalPage).ToList()
            };
        }

        public async Task SaveSessionJournalAsync(
            Guid sessionId,
            Guid gameMasterUserAccountId,
            SessionJournalSaveRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await EnsureGameMasterOwnsSessionAsync(context, sessionId, gameMasterUserAccountId);

            request ??= new SessionJournalSaveRequestDto();

            await SyncJournalScopeAsync(context, sessionId, isPublic: false, request.PrivatePages);
            await SyncJournalScopeAsync(context, sessionId, isPublic: true, request.PublicPages);

            await context.SaveChangesAsync();
            await _sessionNotifier.NotifyJournalChangedAsync(sessionId);
        }

        public async Task<SessionJournalViewDto?> GetPublicSessionJournalAsync(Guid playerSessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var playerSession = await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.Id == playerSessionId);

            if (playerSession == null)
                return null;

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == playerSession.SessionId);

            if (session == null)
                return null;

            var hasChanges = await EnsureJournalHasAtLeastOnePageAsync(context, session.Id, isPublic: true);
            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }

            var publicPages = await context.SessionJournalPages
                .AsNoTracking()
                .Where(x => x.SessionId == session.Id && x.IsPublic)
                .OrderBy(x => x.PageNumber)
                .ToListAsync();

            return new SessionJournalViewDto
            {
                SessionId = session.Id,
                SessionName = session.SessionName,
                PublicPages = publicPages.Select(MapJournalPage).ToList()
            };
        }

        private static SessionJournalPageDto MapJournalPage(SessionJournalPage page)
        {
            return new SessionJournalPageDto
            {
                JournalPageId = page.Id,
                PageNumber = page.PageNumber,
                Title = page.Title,
                ContentHtml = page.ContentHtml,
                IsVisible = page.IsVisible
            };
        }

        private async Task<bool> EnsureJournalHasAtLeastOnePageAsync(
            RollocracyDbContext context,
            Guid sessionId,
            bool isPublic)
        {
            var hasAnyPage = await context.SessionJournalPages
                .AsNoTracking()
                .AnyAsync(x => x.SessionId == sessionId && x.IsPublic == isPublic);

            if (hasAnyPage)
                return false;

            context.SessionJournalPages.Add(new SessionJournalPage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                IsPublic = isPublic,
                PageNumber = 1,
                Title = string.Empty,
                ContentHtml = string.Empty,
                IsVisible = isPublic,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            return true;
        }

        private async Task SyncJournalScopeAsync(
            RollocracyDbContext context,
            Guid sessionId,
            bool isPublic,
            List<SessionJournalPageDto>? incomingPages)
        {
            incomingPages ??= new List<SessionJournalPageDto>();

            if (incomingPages.Count == 0)
            {
                incomingPages.Add(new SessionJournalPageDto
                {
                    PageNumber = 1,
                    Title = string.Empty,
                    ContentHtml = string.Empty,
                    IsVisible = isPublic
                });
            }

            var currentPages = await context.SessionJournalPages
                .Where(x => x.SessionId == sessionId && x.IsPublic == isPublic)
                .OrderBy(x => x.PageNumber)
                .ToListAsync();

            var incomingIds = incomingPages
                .Where(x => x.JournalPageId.HasValue && x.JournalPageId.Value != Guid.Empty)
                .Select(x => x.JournalPageId!.Value)
                .ToHashSet();

            var toDelete = currentPages
                .Where(x => !incomingIds.Contains(x.Id))
                .ToList();

            if (toDelete.Count > 0)
            {
                context.SessionJournalPages.RemoveRange(toDelete);
            }

            for (var i = 0; i < incomingPages.Count; i++)
            {
                var dto = incomingPages[i];
                var title = (dto.Title ?? string.Empty).Trim();
                var contentHtml = SanitizeJournalHtml(dto.ContentHtml);
                var isVisible = isPublic ? true : dto.IsVisible;

                SessionJournalPage? entity = null;

                if (dto.JournalPageId.HasValue && dto.JournalPageId.Value != Guid.Empty)
                {
                    entity = currentPages.FirstOrDefault(x => x.Id == dto.JournalPageId.Value);
                }

                if (entity is null)
                {
                    entity = new SessionJournalPage
                    {
                        Id = Guid.NewGuid(),
                        SessionId = sessionId,
                        IsPublic = isPublic,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    context.SessionJournalPages.Add(entity);
                }

                entity.PageNumber = i + 1;
                entity.Title = title;
                entity.ContentHtml = contentHtml;
                entity.IsVisible = isVisible;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private static string SanitizeJournalHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var value = html;

            value = Regex.Replace(value, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            value = Regex.Replace(
                value,
                @"<(script|style|iframe|object|embed|form|input|button|textarea|select|meta|link)\b[^>]*>.*?</\1>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            value = Regex.Replace(
                value,
                @"<(script|style|iframe|object|embed|form|input|button|textarea|select|meta|link)\b[^>]*/?>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            value = Regex.Replace(
                value,
                @"\son\w+\s*=\s*(""[^""]*""|'[^']*')",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            value = Regex.Replace(value, @"javascript\s*:", string.Empty, RegexOptions.IgnoreCase);

            value = Regex.Replace(
                value,
                @"<img\b[^>]*>",
                match => SanitizeJournalImageTag(match.Value),
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return value.Trim();
        }

        private static string SanitizeJournalImageTag(string imageTag)
        {
            var srcMatch = Regex.Match(
                imageTag,
                @"\bsrc\s*=\s*(?:""(?<src>[^""]+)""|'(?<src>[^']+)'|(?<src>[^\s>]+))",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!srcMatch.Success)
                return string.Empty;

            if (!TryNormalizeJournalImageUrl(srcMatch.Groups["src"].Value, out var normalizedImageUrl))
                return string.Empty;

            return $"<img src=\"{WebUtility.HtmlEncode(normalizedImageUrl)}\" alt=\"\" />";
        }

        private static bool TryNormalizeJournalImageUrl(string? rawUrl, out string normalizedImageUrl)
        {
            normalizedImageUrl = string.Empty;

            if (string.IsNullOrWhiteSpace(rawUrl))
                return false;

            var trimmed = rawUrl.Trim();

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                return false;

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            var extension = Path.GetExtension(uri.AbsolutePath);
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".avif" };

            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return false;

            normalizedImageUrl = uri.ToString();
            return true;
        }

        private static int NormalizeSessionCapacity(int rawValue) => Math.Clamp(rawValue, 0, 5000);

        private string GenerateSessionSlug(string sessionName)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                return Guid.NewGuid().ToString("N")[..8];

            var slug = sessionName.Trim().ToLowerInvariant();
            slug = slug.Replace(" ", "-");
            slug = slug.Replace("'", "-");
            slug = slug.Replace("\"", "-");
            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            slug = slug.Trim('-');
            if (string.IsNullOrWhiteSpace(slug))
                slug = Guid.NewGuid().ToString("N")[..8];
            return slug;
        }
    }
}