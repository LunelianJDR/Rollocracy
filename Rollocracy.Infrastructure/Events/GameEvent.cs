using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Infrastructure.Events
{
    /// Représente un événement survenu dans le jeu.
    /// Utilisé pour l'historique et les replays.
    public class GameEvent
    {
        /// Identifiant unique
        public Guid Id { get; set; }

        /// Session concernée
        public Guid SessionId { get; set; }

        /// Type d'événement
        /// Exemple : DiceRolled
        public string EventType { get; set; } = string.Empty;

        /// Données de l'événement en JSON
        public string EventData { get; set; } = string.Empty;

        /// Date de création
        public DateTime CreatedAt { get; set; }
    }
}
