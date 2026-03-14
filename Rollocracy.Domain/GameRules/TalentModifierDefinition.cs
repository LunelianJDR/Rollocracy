
using System;

namespace Rollocracy.Domain.GameRules
{
    public class TalentModifierDefinition
    {
        public Guid Id { get; set; }

        public Guid TalentDefinitionId { get; set; }

        public ModifierTargetType TargetType { get; set; }

        public Guid TargetId { get; set; }

        public int AddValue { get; set; }

        public TalentDefinition? TalentDefinition { get; set; }
    }
}
