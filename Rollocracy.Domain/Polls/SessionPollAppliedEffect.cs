using System;
using Rollocracy.Domain.GameTests;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollAppliedEffect
    {
        public Guid Id { get; set; }

        public Guid SessionPollId { get; set; }

        public Guid CharacterId { get; set; }

        public Guid SessionPollVoteId { get; set; }

        public Guid SessionPollOptionId { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public int PreviousValue { get; set; }

        public int NewValue { get; set; }

        public bool PreviousIsAlive { get; set; }

        public bool NewIsAlive { get; set; }

        public DateTime? PreviousDiedAtUtc { get; set; }

        public DateTime? NewDiedAtUtc { get; set; }

        public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    }
}