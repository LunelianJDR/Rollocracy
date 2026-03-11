using System;

namespace Rollocracy.Domain.GameRules
{
    public class CharacterTraitValue
    {
        public Guid Id { get; set; }

        // Personnage concerné
        public Guid CharacterId { get; set; }

        // Type d'attribut concerné
        public Guid TraitDefinitionId { get; set; }

        // Option choisie
        public Guid TraitOptionId { get; set; }
    }
}