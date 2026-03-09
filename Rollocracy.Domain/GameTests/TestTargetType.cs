using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameTests
{
    /// Définit qui doit effectuer le test
    public enum TestTargetType
    {
        /// Tous les joueurs
        AllPlayers,

        /// Joueurs en ligne uniquement
        OnlinePlayers,

        /// Liste spécifique de joueurs
        SpecificPlayers
    }
}
