using System;

namespace Rollocracy.Domain.GameRules
{
    public class GaugeDefinition
    {
        public Guid Id { get; set; }

        // Système de jeu auquel appartient cette jauge
        public Guid GameSystemId { get; set; }

        // Nom de la jauge
        // Exemples : Vie, Expérience, Argent, Mental
        public string Name { get; set; } = string.Empty;

        // Valeur minimale autorisée
        public int MinValue { get; set; }

        // Valeur maximale autorisée
        public int MaxValue { get; set; }

        // Valeur par défaut lors de la création du personnage
        public int DefaultValue { get; set; }

        // Si true, atteindre 0 ou moins tue le personnage
        public bool IsHealthGauge { get; set; }
    }
}