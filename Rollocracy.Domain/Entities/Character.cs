using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Entities
{

    public class Character
    {
        public Guid Id { get; set; }

        public Guid PlayerSessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Health { get; set; }

        public int MaxHealth { get; set; }

        public int Experience { get; set; }

        public int Level { get; set; }

        public bool IsAlive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
