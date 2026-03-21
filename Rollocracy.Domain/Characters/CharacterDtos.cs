using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Characters
{
    public class SessionPublicStatsDto
    {
        public int ConnectedPlayersCount { get; set; }
        public int AliveCharactersCount { get; set; }
        public int TotalCharactersCount { get; set; }
    }

    public class CharacterListItemDto
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public DateTime? DiedAtUtc { get; set; }
    }

    public class CharacterAttributeLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class CharacterDerivedStatLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class CharacterMetricLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class CharacterTraitLineDto
    {
        public string TraitName { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;
    }

    public class CharacterGaugeLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool IsHealthGauge { get; set; }
    }

    public class CharacterNameLineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CharacterSheetDto
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public DateTime? DiedAtUtc { get; set; }

        public List<CharacterAttributeLineDto> Attributes { get; set; } = new();
        public List<CharacterDerivedStatLineDto> DerivedStats { get; set; } = new();
        public List<CharacterMetricLineDto> Metrics { get; set; } = new();
        public List<CharacterTraitLineDto> Traits { get; set; } = new();
        public List<CharacterGaugeLineDto> Gauges { get; set; } = new();
        public List<CharacterNameLineDto> Talents { get; set; } = new();
        public List<CharacterNameLineDto> Items { get; set; } = new();
    }

    public class CharacterCreationAttributeDto
    {
        public Guid AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
    }

    public class CharacterCreationTraitOptionDto
    {
        public Guid TraitOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CharacterCreationTraitDto
    {
        public Guid TraitDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CharacterCreationTraitOptionDto> Options { get; set; } = new();
    }

    public class CharacterCreationTemplateDto
    {
        public Guid PlayerSessionId { get; set; }
        public Guid SessionId { get; set; }
        public Guid GameSystemId { get; set; }
        public string GameSystemName { get; set; } = string.Empty;
        public List<CharacterCreationAttributeDto> Attributes { get; set; } = new();
        public List<CharacterCreationTraitDto> Traits { get; set; } = new();
    }

    public class PlayerRoomStateDto
    {
        public Guid PlayerSessionId { get; set; }
        public Guid SessionId { get; set; }
        public bool HasAssignedGameSystem { get; set; }
        public Guid? SessionGameSystemId { get; set; }
        public CharacterSheetDto? AliveCharacter { get; set; }
        public List<CharacterListItemDto> DeadCharacters { get; set; } = new();
        public bool CanCreateNewCharacter { get; set; }
        public DateTime? CanCreateNewCharacterAtUtc { get; set; }
        public SessionPublicStatsDto SessionStats { get; set; } = new();
        public SessionSpecialRole SpecialRole { get; set; } = SessionSpecialRole.None;
        public bool CanViewSessionCharacters { get; set; }
        public bool CanEditSessionCharacters { get; set; }
        public bool CanEditSessionGameSystem { get; set; }
    }

    public class SessionCharacterSummaryDto
    {
        public Guid CharacterId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public bool IsOnline { get; set; }
        public bool IsNpc { get; set; }

        public List<CharacterAttributeLineDto> Attributes { get; set; } = new();
        public List<CharacterDerivedStatLineDto> DerivedStats { get; set; } = new();
        public List<CharacterMetricLineDto> Metrics { get; set; } = new();
        public List<CharacterTraitLineDto> Traits { get; set; } = new();
        public List<CharacterGaugeLineDto> Gauges { get; set; } = new();
        public List<CharacterNameLineDto> Talents { get; set; } = new();
        public List<CharacterNameLineDto> Items { get; set; } = new();
    }

    public class EditableCharacterAttributeDto
    {
        public Guid AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Value { get; set; }
    }

    public class EditableCharacterGaugeDto
    {
        public Guid GaugeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Value { get; set; }
        public bool IsHealthGauge { get; set; }
    }

    public class EditableCharacterTraitOptionDto
    {
        public Guid TraitOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EditableCharacterTraitDto
    {
        public Guid TraitDefinitionId { get; set; }
        public string TraitName { get; set; } = string.Empty;
        public Guid? SelectedOptionId { get; set; }
        public List<EditableCharacterTraitOptionDto> Options { get; set; } = new();
    }

    public class EditableCharacterGrantDto
    {
        public Guid DefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class EditableCharacterDto
    {
        public Guid CharacterId { get; set; }
        public Guid PlayerSessionId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public DateTime? DiedAtUtc { get; set; }
        public List<EditableCharacterAttributeDto> Attributes { get; set; } = new();
        public List<CharacterDerivedStatLineDto> DerivedStats { get; set; } = new();
        public List<CharacterMetricLineDto> Metrics { get; set; } = new();
        public List<EditableCharacterTraitDto> Traits { get; set; } = new();
        public List<EditableCharacterGaugeDto> Gauges { get; set; } = new();
        public List<EditableCharacterGrantDto> Talents { get; set; } = new();
        public List<EditableCharacterGrantDto> Items { get; set; } = new();
    }

    public class UpdateCharacterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public List<EditableCharacterAttributeDto> Attributes { get; set; } = new();
        public List<EditableCharacterTraitDto> Traits { get; set; } = new();
        public List<EditableCharacterGaugeDto> Gauges { get; set; } = new();
        public List<Guid> SelectedTalentIds { get; set; } = new();
        public List<Guid> SelectedItemIds { get; set; } = new();
    }

    public class SessionGaugeDto
    {
        public Guid SessionGaugeId {  get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int CurrentValue { get; set; }
    }

    public class CreateSessionGaugeRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int CurrentValue { get; set; }
    }

    public class CharacterUpdateResultDto
    {
        public bool ResurrectionBlocked { get; set; }
    }

    public class RandomDrawResultCharacterDto
    {
        public Guid CharacterId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public bool IsNpc { get; set; }
    }

    public class RandomDrawHistoryItemDto
    {
        public Guid DrawId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<RandomDrawResultCharacterDto> Results { get; set; } = new();
    }

    public class RandomDrawEditorDto
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string SuggestedName { get; set; } = string.Empty;

        public List<GroupedNamedReferenceDto> TraitOptions { get; set; } = new();
        public List<NamedReferenceDto> Talents { get; set; } = new();
        public List<NamedReferenceDto> Items { get; set; } = new();
        public List<NamedReferenceDto> BaseAttributes { get; set; } = new();
        public List<NamedReferenceDto> Gauges { get; set; } = new();
        public List<NamedReferenceDto> DerivedStats { get; set; } = new();
        public List<NamedReferenceDto> Metrics { get; set; } = new();

        public List<RandomDrawHistoryItemDto> RecentDraws { get; set; } = new();
    }

    public class RandomDrawRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public int DrawCount { get; set; } = 1;
        public CharacterTargetFilterDto Filter { get; set; } = new();
    }

    public class RandomDrawResultDto
    {
        public Guid DrawId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<RandomDrawResultCharacterDto> Results { get; set; } = new();
    }
}
