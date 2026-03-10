using Rollocracy.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Interfaces
{
    public interface ISessionService
    {
        Task<Session> CreateSessionAsync(Guid streamerId, Guid gameSystemId, string sessionName);

        Task<PlayerSession> JoinSessionAsync(string sessionCode, string playerName);

        Task<List<PlayerSession>> GetPlayersAsync(Guid sessionId);

        Task<Session?> GetSessionByCodeAsync(string sessionCode);

        Task<PlayerSession?> GetPlayerByIdAsync(Guid playerId);
    }
}
