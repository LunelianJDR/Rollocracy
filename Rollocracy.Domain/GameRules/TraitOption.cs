using System;

namespace Rollocracy.Domain.GameRules
{
    public class TraitOption
    {
        public Guid Id { get; set; }

        // Attribut de choix auquel appartient cette option
        public Guid TraitDefinitionId { get; set; }

        // Nom de l'option
        // Exemples : Guerrier, Mage, Elfe, Humain
        public string Name { get; set; } = string.Empty;
    }
}