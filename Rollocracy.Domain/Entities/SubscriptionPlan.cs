using System;

namespace Rollocracy.Domain.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }

        // Code stable utilisé côté code / localisation
        public string Code { get; set; } = string.Empty;

        // Nom stocké en base pour l’admin futur / lisibilité
        public string Name { get; set; } = string.Empty;

        public decimal MonthlyPriceTtc { get; set; }

        public int MaxPlayersPerSession { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}