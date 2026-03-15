using System;

namespace Rollocracy.Domain.Characters
{
    // Modificateur persistant appliqué directement à un personnage.
    // Il sert de fondation commune pour les distributions MJ, tests et sondages.
    public class CharacterModifier
    {
        public Guid Id { get; set; }

        public Guid CharacterId { get; set; }

        public CharacterEffectTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public int AddValue { get; set; }

        public CharacterEffectSourceType SourceType { get; set; }

        public Guid SourceId { get; set; }

        public string SourceNameSnapshot { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
