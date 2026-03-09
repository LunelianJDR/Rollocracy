using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Entities
{
    public class Streamer
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string TwitchId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
