using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameRules
{
    /// <summary>
    /// Représente une caractéristique définie dans un système de jeu.
    /// Exemple : Force, Agilité, Intelligence.
    /// Le MJ peut configurer ces attributs dans son GameSystem.
    public class AttributeDefinition
    {
        /// Identifiant unique de l'attribut
        public Guid Id { get; set; }

        /// Système de jeu auquel appartient cet attribut
        public Guid GameSystemId { get; set; }

        /// Nom de la caractéristique (Force, Agilité, etc.)
        public string Name { get; set; } = string.Empty;

        /// Valeur minimale autorisée pour cet attribut
        public int MinValue { get; set; }

        /// Valeur maximale autorisée pour cet attribut
        public int MaxValue { get; set; }

        /// Valeur par défaut lors de la création d'un personnage
        public int DefaultValue { get; set; }
    }
}
