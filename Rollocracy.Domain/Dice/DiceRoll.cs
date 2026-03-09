using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Dice
{
    /// Représente un jet de dés effectué par un joueur.
    public class DiceRoll
    {
        /// Identifiant du jet
        public Guid Id { get; set; }

        /// Joueur qui effectue le jet
        public Guid PlayerSessionId { get; set; }

        /// Test utilisé
        public Guid DiceDefinitionId { get; set; }

        /// Valeurs individuelles des dés
        /// Exemple : [2,5]
        public List<int> DiceResults { get; set; } = new();

        /// Bonus total appliqué
        public int Modifier { get; set; }

        /// Résultat final
        public int Total { get; set; }

        /// Indique si le test est réussi
        public bool IsSuccess { get; set; }

        /// Date du jet
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
