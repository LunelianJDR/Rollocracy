using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;

        public SessionService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
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

            if (!gameMasterUser.IsGameMaster)
                throw new Exception(_localizer["Backend_OnlyGameMastersCanCreateSession"]);

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
                SessionName = sessionName,
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
                IsGameMaster = true
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
            string playerName,
            bool isGameMaster)
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

            if (!string.IsNullOrWhiteSpace(session.SessionPassword))
            {
                if (session.SessionPassword != sessionPassword)
                    throw new Exception(_localizer["Backend_InvalidSessionPassword"]);
            }

            var existingPlayerSession = await context.PlayerSessions
                .FirstOrDefaultAsync(p =>
                    p.SessionId == session.Id &&
                    p.UserAccountId == userAccountId);

            if (existingPlayerSession != null)
                return existingPlayerSession;

            var playerSession = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserAccountId = userAccountId,
                PlayerName = playerName,
                IsGameMaster = isGameMaster
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
                .FirstOrDefaultAsync(s =>
                    s.Id == sessionId &&
                    s.GameMasterUserAccountId == gameMasterUserAccountId);

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
                .Join(
                    context.PlayerSessions,
                    character => character.PlayerSessionId,
                    playerSession => playerSession.Id,
                    (character, playerSession) => new { character, playerSession })
                .Where(x => x.playerSession.SessionId == sessionId)
                .CountAsync();
        }

        public async Task AssignGameSystemToSessionAsync(Guid sessionId, Guid gameMasterUserAccountId, Guid gameSystemId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .FirstOrDefaultAsync(s =>
                    s.Id == sessionId &&
                    s.GameMasterUserAccountId == gameMasterUserAccountId);

            if (session == null)
                throw new Exception(_localizer["Backend_SessionNotFound"]);

            var system = await context.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                    gs.Id == gameSystemId &&
                    gs.OwnerUserAccountId == gameMasterUserAccountId);

            if (system == null)
                throw new Exception(_localizer["Backend_GameSystemNotFound"]);

            session.GameSystemId = gameSystemId;

            await context.SaveChangesAsync();
        }

        private string GenerateSessionSlug(string sessionName)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                return Guid.NewGuid().ToString("N")[..8];

            var slug = sessionName.Trim().ToLowerInvariant();

            slug = slug.Replace(" ", "-");
            slug = slug.Replace("'", "-");
            slug = slug.Replace("\"", "-");
            slug = slug.Replace(".", "-");
            slug = slug.Replace(",", "-");
            slug = slug.Replace(";", "-");
            slug = slug.Replace(":", "-");
            slug = slug.Replace("/", "-");
            slug = slug.Replace("\\", "-");
            slug = slug.Replace("?", "-");
            slug = slug.Replace("!", "-");
            slug = slug.Replace("&", "-");
            slug = slug.Replace("(", "-");
            slug = slug.Replace(")", "-");

            while (slug.Contains("--"))
            {
                slug = slug.Replace("--", "-");
            }

            slug = slug.Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
                slug = Guid.NewGuid().ToString("N")[..8];

            return slug;
        }
    }
}