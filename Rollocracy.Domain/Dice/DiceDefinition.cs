using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Dice
{
    /// Définit les règles d'un jet de dés configuré par le MJ.
    public class DiceDefinition
    {
        /// Identifiant unique de la règle de jet
        public Guid Id { get; set; }

        /// Session dans laquelle ce test est utilisé
        public Guid SessionId { get; set; }

        /// Nom du test affiché aux joueurs
        /// Exemple : "Test de Force"
        public string Name { get; set; } = string.Empty;

        /// Nombre de dés à lancer
        /// Exemple : 2 pour "2d6"
        public int DiceCount { get; set; }

        /// Nombre de faces du dé
        /// Exemple : 6 pour "d6"
        public int DiceSides { get; set; }

        /// Bonus fixe appliqué au jet
        /// Exemple : +2
        public int FlatModifier { get; set; }

        /// Attribut utilisé comme modificateur
        /// Exemple : Force
        public Guid? AttributeModifierId { get; set; }

        /// Valeur seuil à atteindre
        /// Exemple : 7
        public int TargetValue { get; set; }

        /// Type de comparaison
        /// (>=, <=)
        public ComparisonType ComparisonType { get; set; }
    }
}
