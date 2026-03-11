using System;

namespace Rollocracy.Domain.Entities
{
    public class Character
    {
        public Guid Id { get; set; }

        public Guid PlayerSessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        // Le personnage reste sauvegardé, mais n'est plus actif si false
        public bool IsAlive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}