using System;

namespace Rollocracy.Domain.GameRules
{
    public class MetricComponent
    {
        public Guid Id { get; set; }

        public Guid MetricDefinitionId { get; set; }

        public Guid AttributeDefinitionId { get; set; }

        // Même principe que pour les compétences calculées.
        public int Weight { get; set; } = 100;
    }
}
