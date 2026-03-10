using Microsoft.EntityFrameworkCore;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;

        public SessionService(IDbContextFactory<RollocracyDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Session> CreateSessionAsync(Guid streamerId, Guid gameSystemId, string sessionName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = new Session
            {
                Id = Guid.NewGuid(),
                StreamerId = streamerId,
                GameSystemId = gameSystemId,
                SessionName = sessionName,
                SessionCode = GenerateSessionCode(),
                IsActive = true
            };

            context.Sessions.Add(session);

            var gm = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                PlayerName = "MJ",
                IsGameMaster = true
            };

            context.PlayerSessions.Add(gm);

            await context.SaveChangesAsync();

            return session;
        }

        public async Task<PlayerSession> JoinSessionAsync(string sessionCode, string playerName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var session = await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionCode == sessionCode);

            if (session == null)
                throw new Exception("Session not found");

            var player = new PlayerSession
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                PlayerName = playerName,
                IsGameMaster = false
            };

            context.PlayerSessions.Add(player);

            await context.SaveChangesAsync();

            return player;
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

        public async Task<Session?> GetSessionByCodeAsync(string sessionCode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionCode == sessionCode);
        }

        public async Task<PlayerSession?> GetPlayerByIdAsync(Guid playerId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.PlayerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        private string GenerateSessionCode()
        {
            return Guid.NewGuid().ToString("N")[..6].ToUpper();
        }
    }
}