using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollWeightRule
    {
        public Guid Id { get; set; }

        public Guid SessionPollId { get; set; }

        public Guid TraitDefinitionId { get; set; }

        public Guid TraitOptionId { get; set; }

        public decimal WeightBonus { get; set; }
    }
}