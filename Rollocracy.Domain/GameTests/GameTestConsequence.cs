using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameTests
{
    /// Représente une conséquence appliquée suite à un test.
    /// Permet de revenir en arrière si le MJ annule le test.
    public class GameTestConsequence
    {
        /// Identifiant unique
        public Guid Id { get; set; }

        /// Test concerné
        public Guid GameTestId { get; set; }

        /// Personnage affecté
        public Guid CharacterId { get; set; }

        /// Type de conséquence (ex : perte de PV)
        public ConsequenceType Type { get; set; }

        /// Valeur appliquée
        /// Exemple : -1 PV
        public int Value { get; set; }

        /// Valeur précédente pour rollback
        public int PreviousValue { get; set; }
    }
}
