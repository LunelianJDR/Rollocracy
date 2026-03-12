using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollVote
    {
        public Guid Id { get; set; }

        public Guid SessionPollId { get; set; }

        public Guid SessionPollOptionId { get; set; }

        public Guid PlayerSessionId { get; set; }

        public Guid CharacterId { get; set; }

        public decimal VoteWeight { get; set; } = 1.00m;

        public DateTime VotedAtUtc { get; set; } = DateTime.UtcNow;
    }
}