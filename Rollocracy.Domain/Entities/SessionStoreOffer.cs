using System;

namespace Rollocracy.Domain.Entities
{
    public enum SessionStoreOfferType
    {
        Talent = 0,
        Item = 1
    }

    public class SessionStoreOffer
    {
        public Guid Id { get; set; }

        public Guid SessionStoreId { get; set; }

        public SessionStoreOfferType OfferType { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public Guid CurrencyGaugeDefinitionId { get; set; }

        public int Cost { get; set; }

        public int DisplayOrder { get; set; }
    }
}
