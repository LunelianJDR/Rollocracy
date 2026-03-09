using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rollocracy.Domain.Dice;

namespace Rollocracy.Domain.GameTests
{
    /// Représente un test demandé par le MJ.
    public class GameTest
    {
        /// Identifiant unique du test
        public Guid Id { get; set; }

        /// Session dans laquelle le test est lancé
        public Guid SessionId { get; set; }

        /// Attribut utilisé pour ce test (Force, Agilité, etc.)
        public Guid AttributeDefinitionId { get; set; }

        /// Formule de dés utilisée (ex : "1d20", "2d6")
        public string DiceFormula { get; set; } = string.Empty;

        /// Modificateur appliqué au résultat
        public int Modifier { get; set; }

        /// Valeur cible à atteindre
        public int TargetValue { get; set; }

        /// Type de comparaison (<= ou >=)
        public ComparisonType Comparison { get; set; }

        /// Date de création du test
        public DateTime CreatedAt { get; set; }

        /// Durée du timer en secondes
        public int DurationSeconds { get; set; }

        /// Permet d'annuler les conséquences si le MJ annule le test
        public bool IsCancelled { get; set; }

        public TestTargetType TargetType { get; set; }

        public GameTestStatus Status { get; set; }
    }
}
