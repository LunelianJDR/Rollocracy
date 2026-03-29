using System;

namespace Rollocracy.Domain.Entities
{
    public class UserSubscription
    {
        public Guid Id { get; set; }

        public Guid UserAccountId { get; set; }

        // Offre actuellement active
        public Guid CurrentPlanId { get; set; }

        // Offre programmée au renouvellement (downgrade ou désabonnement vers gratuit)
        public Guid? PendingPlanId { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime NextRenewalAtUtc { get; set; }

        // true = retour au gratuit au renouvellement
        public bool CancelAtRenewal { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}