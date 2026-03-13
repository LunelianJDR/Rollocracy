using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.GameRules
{
    public class GameSystemImpactSessionDto
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
    }

    public class EditableAttributeDefinitionDto
    {
        public Guid? AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
        public BaseValueGenerationMode DefaultValueMode { get; set; } = BaseValueGenerationMode.Fixed;
        public int DefaultValueDiceCount { get; set; } = 1;
        public int DefaultValueDiceSides { get; set; } = 6;
        public int DefaultValueFlatBonus { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class EditableDerivedStatComponentDto
    {
        public Guid? DerivedStatComponentId { get; set; }
        public Guid AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public int Weight { get; set; } = 100;
        public bool IsDeleted { get; set; }
    }

    public class EditableDerivedStatDefinitionDto
    {
        public Guid? DerivedStatDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public ComputedValueRoundMode RoundMode { get; set; } = ComputedValueRoundMode.Ceiling;
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public List<EditableDerivedStatComponentDto> Components { get; set; } = new();
    }

    public class EditableGaugeDefinitionDto
    {
        public Guid? GaugeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
        public bool IsHealthGauge { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class EditableTraitOptionDto
    {
        public Guid? TraitOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    public class EditableTraitDefinitionDto
    {
        public Guid? TraitDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public List<EditableTraitOptionDto> Options { get; set; } = new();
    }

    public class BaseAttributeReferenceDto
    {
        public Guid AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class GameSystemEditorDto
    {
        public Guid GameSystemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestResolutionMode TestResolutionMode { get; set; }

        public bool IsLockedToSessionCopy { get; set; }

        public bool CanUndoLastChange { get; set; }

        public List<GameSystemImpactSessionDto> ImpactedSessions { get; set; } = new();

        public List<BaseAttributeReferenceDto> AvailableBaseAttributes { get; set; } = new();
        public List<EditableAttributeDefinitionDto> Attributes { get; set; } = new();
        public List<EditableDerivedStatDefinitionDto> DerivedStats { get; set; } = new();
        public List<EditableTraitDefinitionDto> Traits { get; set; } = new();
        public List<EditableGaugeDefinitionDto> Gauges { get; set; } = new();
    }

    public class GameSystemApplyChangesRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestResolutionMode TestResolutionMode { get; set; }

        public bool ConfirmSharedSystemChanges { get; set; }

        public List<EditableAttributeDefinitionDto> Attributes { get; set; } = new();
        public List<EditableDerivedStatDefinitionDto> DerivedStats { get; set; } = new();
        public List<EditableTraitDefinitionDto> Traits { get; set; } = new();
        public List<EditableGaugeDefinitionDto> Gauges { get; set; } = new();
    }
}
