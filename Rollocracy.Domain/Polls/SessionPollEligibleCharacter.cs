using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollEligibleCharacter
    {
        public Guid Id { get; set; }

        public Guid SessionPollId { get; set; }

        public Guid CharacterId { get; set; }
    }
}