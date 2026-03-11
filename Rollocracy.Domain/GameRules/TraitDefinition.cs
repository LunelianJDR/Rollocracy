using System;

namespace Rollocracy.Domain.GameRules
{
    public class TraitDefinition
    {
        public Guid Id { get; set; }

        // Système de jeu auquel appartient cet attribut de choix
        public Guid GameSystemId { get; set; }

        // Nom du type d'attribut
        // Exemples : Classe, Race, Background
        public string Name { get; set; } = string.Empty;
    }
}