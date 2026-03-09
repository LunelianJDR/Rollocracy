using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameTests
{
    /// Etat d'un test dans la session
    public enum GameTestStatus
    {
        Pending,
        Running,
        Completed,
        Cancelled
    }
}
