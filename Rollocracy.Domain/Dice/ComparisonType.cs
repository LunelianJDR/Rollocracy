using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Dice
{
    /// Type de comparaison utilisé pour déterminer la réussite d'un jet.
    public enum ComparisonType
    {
        /// Le résultat doit être supérieur ou égal à la cible.
        /// Exemple : total >= 7
        GreaterOrEqual,

        /// Le résultat doit être inférieur ou égal à la cible.
        /// Exemple : d20 <= Force
        LessOrEqual
    }
}
