using System;

namespace Rollocracy.Domain.GameTests
{
    public class PlayerTestRoll
    {
        public Guid Id { get; set; }

        public Guid GameTestId { get; set; }

        public Guid CharacterId { get; set; }

        public Guid PlayerSessionId { get; set; }

        public string CharacterNameSnapshot { get; set; } = string.Empty;

        public string PlayerNameSnapshot { get; set; } = string.Empty;

        public int AttributeValueSnapshot { get; set; }

        public int EffectiveAttributeValue { get; set; }

        public string DiceResultsJson { get; set; } = string.Empty;

        public int DiceTotal { get; set; }

        public int FinalValue { get; set; }

        public bool IsSuccess { get; set; }

        public bool HasRolled { get; set; }

        public bool IsAutoRolled { get; set; }

        public DateTime? RolledAtUtc { get; set; }
    }
}