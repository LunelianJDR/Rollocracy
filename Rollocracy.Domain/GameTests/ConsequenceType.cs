using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameTests
{
    /// Types de conséquences possibles suite à un test
    public enum ConsequenceType
    {
        /// Modification des points de vie
        HealthChange,

        /// Gain d'expérience
        ExperienceGain,

        /// Mort du personnage
        CharacterDeath
    }
}
