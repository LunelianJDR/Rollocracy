
using System;

namespace Rollocracy.Domain.Characters
{
    public class CharacterTalent
    {
        public Guid Id { get; set; }

        public Guid CharacterId { get; set; }

        public Guid TalentDefinitionId { get; set; }
    }
}
