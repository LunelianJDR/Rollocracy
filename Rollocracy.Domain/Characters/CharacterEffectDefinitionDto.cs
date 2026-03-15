using System;

namespace Rollocracy.Domain.Characters
{
    // Définition unifiée d'un effet à appliquer à un ou plusieurs personnages.
    public class CharacterEffectDefinitionDto
    {
        public CharacterEffectTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public string TargetName { get; set; } = string.Empty;

        public CharacterEffectOperationType OperationType { get; set; }

        public int Value { get; set; }
    }
}
