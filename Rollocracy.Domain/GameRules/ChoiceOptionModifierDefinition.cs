using System;

namespace Rollocracy.Domain.GameRules
{
    public class ChoiceOptionModifierDefinition
    {
        public Guid Id { get; set; }

        public Guid TraitOptionId { get; set; }

        public ModifierTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public int AddValue { get; set; }
    }
}
