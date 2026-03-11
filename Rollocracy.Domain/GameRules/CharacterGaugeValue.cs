using System;

namespace Rollocracy.Domain.GameRules
{
    public class CharacterGaugeValue
    {
        public Guid Id { get; set; }

        // Personnage concerné
        public Guid CharacterId { get; set; }

        // Définition de la jauge concernée
        public Guid GaugeDefinitionId { get; set; }

        // Valeur actuelle de la jauge
        public int Value { get; set; }
    }
}