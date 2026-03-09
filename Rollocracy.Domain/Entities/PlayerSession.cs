using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Entities
{
    public class PlayerSession
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public string PlayerName { get; set; } = string.Empty;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
