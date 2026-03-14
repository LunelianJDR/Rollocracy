using System;

namespace Rollocracy.Domain.GameRules
{
    public class MetricDefinition
    {
        public Guid Id { get; set; }

        public Guid GameSystemId { get; set; }

        public string Name { get; set; } = string.Empty;

        // Valeur de base ajoutée au calcul pondéré.
        public int BaseValue { get; set; } = 0;

        public int MinValue { get; set; } = 0;

        public int MaxValue { get; set; } = 100;

        public ComputedValueRoundMode RoundMode { get; set; } = ComputedValueRoundMode.Ceiling;

        public int DisplayOrder { get; set; }
    }
}
