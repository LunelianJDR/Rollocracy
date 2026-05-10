using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Entities
{
    public class SessionStoreReferenceOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsConsumable { get; set; }
        public int MaxQuantityPerCharacter { get; set; }
    }

    public class SessionStoreGaugeOptionDto
    {
        public Guid GaugeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SessionStoreOfferEditorDto
    {
        public Guid? OfferId { get; set; }
        public SessionStoreOfferType OfferType { get; set; }
        public Guid TargetDefinitionId { get; set; }
        public Guid CurrencyGaugeDefinitionId { get; set; }
        public int Cost { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SessionStoreEditorDto
    {
        public Guid SessionId { get; set; }
        public bool IsEnabled { get; set; }

        public List<SessionStoreGaugeOptionDto> AvailableGauges { get; set; } = new();
        public List<SessionStoreReferenceOptionDto> AvailableTalents { get; set; } = new();
        public List<SessionStoreReferenceOptionDto> AvailableItems { get; set; } = new();
        public List<SessionStoreOfferEditorDto> Offers { get; set; } = new();
    }

    public class PlayerSessionStoreOfferDto
    {
        public Guid OfferId { get; set; }
        public SessionStoreOfferType OfferType { get; set; }
        public Guid TargetDefinitionId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string CurrencyGaugeName { get; set; } = string.Empty;
        public Guid CurrencyGaugeDefinitionId { get; set; }
        public int Cost { get; set; }
        public bool IsPurchasable { get; set; }
        public bool IsOwned { get; set; }
        public bool IsConsumable { get; set; }
        public int CurrentQuantity { get; set; }
        public int MaxQuantityPerCharacter { get; set; }
    }

    public class PlayerSessionStoreDto
    {
        public bool Exists { get; set; }
        public bool IsEnabled { get; set; }
        public List<SessionStoreGaugeOptionDto> AvailableGauges { get; set; } = new();
        public List<PlayerSessionStoreOfferDto> Offers { get; set; } = new();
    }
}