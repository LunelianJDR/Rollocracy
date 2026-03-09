using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Entities
{
    public class GameSystem
    {
        public Guid Id { get; set; }

        public Guid StreamerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
