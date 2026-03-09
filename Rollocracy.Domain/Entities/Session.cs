using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Entities
{
    public class Session
    {
        public Guid Id { get; set; }

        public Guid StreamerId { get; set; }

        public Guid GameSystemId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
