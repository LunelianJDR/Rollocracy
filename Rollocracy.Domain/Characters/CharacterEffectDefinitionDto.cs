using System;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;

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

        public ConsequenceClampMode ClampMode { get; set; } = ConsequenceClampMode.None;

        public int ClampMinTotal { get; set; } = -100;

        public int ClampMaxTotal { get; set; } = 100;

        public ModifierValueMode ValueMode { get; set; }

        public Guid? SourceMetricId { get; set; }
    }
}