using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;
using Rollocracy.Domain.Characters;

namespace Rollocracy.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;
        private readonly IPresenceTracker _presenceTracker;

        public SessionService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory,
            IPresenceTracker presenceTracker)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
            _presenceTracker = presenceTracker;
        }

        public async Task<Session> CreateSessionAsync(
            Guid gameMasterUserAccountId,
            Guid gameSystemId,
            string sessionName,
            string sessionPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var gameMasterUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == gameMasterUserAccountId);

            if (gameMasterUser == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedJms = NormalizeSessionCapacity(gameMasterUser.MaxPlayersPerSession);

            if (normalizedJms <= 0)
                throw new Exception(_localizer["Backend_OnlyUsersWithPositiveJmsCanCreateSession"]);

            var sessionSlug = GenerateSessionSlug(sessionName);

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
                GameSystemId = null,
                SessionName = sessionName.Trim(),
                SessionSlug = sessionSlug,
                SessionPassword = sessionPassword.Trim(),
                IsActive = true
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
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            if (!session.IsActive)
                throw new Exception(_localizer["Backend_SessionInactive"]);

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

        public async Task SetSessionActiveStateAsync(Guid sessionId, Guid gameMasterUserAccountId, bool isActive)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            session.IsActive = isActive;
            await context.SaveChangesAsync();
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
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs => gs.Id == gameSystemId && gs.OwnerUserAccountId == gameMasterUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            session.GameSystemId = gameSystemId;
            await context.SaveChangesAsync();
        }

        public async Task<bool> CanUserCreateSessionsAsync(Guid userAccountId)
        {
            var maxPlayers = await GetUserMaxPlayersPerSessionAsync(userAccountId);
            return maxPlayers > 0;
        }

        public async Task<int> GetUserMaxPlayersPerSessionAsync(Guid userAccountId)
        {
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
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var trimmedSessionName = sessionName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSessionName))
                throw new Exception(_localizer["Backend_SessionNameRequired"]);

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
                throw new Exception(_localizer["Backend_SessionNotFound"]);
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
