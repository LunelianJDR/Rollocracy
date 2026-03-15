using System;

namespace Rollocracy.Domain.Characters
{
    // Filtre de comparaison sur une valeur calculée ou stockée d'un personnage.
    public class CharacterValueFilterDto
    {
        public CharacterEffectTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public string TargetName { get; set; } = string.Empty;

        public CharacterValueComparisonType ComparisonType { get; set; }

        public int Value { get; set; }
    }
}
