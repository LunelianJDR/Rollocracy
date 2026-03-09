using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameTests
{
    /// Résultat d'un jet de dés pour un joueur donné.
    public class PlayerTestRoll
    {
        /// Identifiant unique
        public Guid Id { get; set; }

        /// Test concerné
        public Guid GameTestId { get; set; }

        /// Joueur concerné
        public Guid PlayerSessionId { get; set; }

        /// Résultat total du jet
        public int TotalResult { get; set; }

        /// Résultat détaillé des dés
        public string DiceDetails { get; set; } = string.Empty;

        /// Indique si le test est réussi
        public bool IsSuccess { get; set; }

        /// Indique si le jet a été fait automatiquement (AFK)
        public bool IsAutoRolled { get; set; }

        /// Date du jet
        public DateTime RolledAt { get; set; }
    }
}
