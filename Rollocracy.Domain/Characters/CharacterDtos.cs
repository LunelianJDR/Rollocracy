using System;
using System.Collections.Generic;

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

    public class CharacterSheetDto
    {
        public Guid CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public DateTime? DiedAtUtc { get; set; }
        public List<CharacterAttributeLineDto> Attributes { get; set; } = new();
        public List<CharacterTraitLineDto> Traits { get; set; } = new();
        public List<CharacterGaugeLineDto> Gauges { get; set; } = new();
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
        public CharacterSheetDto? AliveCharacter { get; set; }
        public List<CharacterListItemDto> DeadCharacters { get; set; } = new();
        public bool CanCreateNewCharacter { get; set; }
        public DateTime? CanCreateNewCharacterAtUtc { get; set; }
        public SessionPublicStatsDto SessionStats { get; set; } = new();
    }

    public class SessionCharacterSummaryDto
    {
        public Guid CharacterId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public bool IsOnline { get; set; }
        public List<CharacterAttributeLineDto> Attributes { get; set; } = new();
        public List<CharacterTraitLineDto> Traits { get; set; } = new();
        public List<CharacterGaugeLineDto> Gauges { get; set; } = new();
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
        public List<EditableCharacterTraitDto> Traits { get; set; } = new();
        public List<EditableCharacterGaugeDto> Gauges { get; set; } = new();
    }

    public class UpdateCharacterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Biography { get; set; } = string.Empty;
        public List<EditableCharacterAttributeDto> Attributes { get; set; } = new();
        public List<EditableCharacterTraitDto> Traits { get; set; } = new();
        public List<EditableCharacterGaugeDto> Gauges { get; set; } = new();
    }

    public class CharacterUpdateResultDto
    {
        public bool ResurrectionBlocked { get; set; }
    }
}