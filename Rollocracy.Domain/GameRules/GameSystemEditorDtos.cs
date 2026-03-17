using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.GameRules
{
    public class GameSystemImpactSessionDto
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
    }

    public class BaseAttributeReferenceDto
    {
        public Guid AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EditableAttributeDefinitionDto
    {
        public Guid? AttributeDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
        public BaseValueGenerationMode DefaultValueMode { get; set; }
        public int DefaultValueFlatBonus { get; set; }
        public int DefaultValueDiceCount { get; set; }
        public int DefaultValueDiceSides { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class EditableDerivedStatComponentDto
    {
        public Guid? DerivedStatComponentId { get; set; }
        public Guid AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public int Weight { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class EditableDerivedStatDefinitionDto
    {
        public Guid? DerivedStatDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public ComputedValueRoundMode RoundMode { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public List<EditableDerivedStatComponentDto> Components { get; set; } = new();
    }

    public class EditableMetricComponentDto
    {
        public Guid? MetricComponentId { get; set; }
        public Guid AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public int Weight { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class EditableMetricFormulaStepDto
    {
        public Guid? MetricFormulaStepId { get; set; }
        public int Order { get; set; }
        public MetricFormulaOperationType OperationType { get; set; }
        public MetricFormulaSourceType SourceType { get; set; }
        public Guid? SourceId { get; set; }
        public decimal ConstantValue { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    public class EditableMetricDefinitionDto
    {
        public Guid? MetricDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TemporaryKey { get; set; } = string.Empty;
        public int BaseValue { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public ComputedValueRoundMode RoundMode { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public List<EditableMetricComponentDto> Components { get; set; } = new();
        public List<EditableMetricFormulaStepDto> FormulaSteps { get; set; } = new();
    }

    public class EditableTraitOptionDto
    {
        public Guid? TraitOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }

        public List<EditableModifierDefinitionDto> Modifiers { get; set; } = new();
    }

    public class EditableTraitDefinitionDto
    {
        public Guid? TraitDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public List<EditableTraitOptionDto> Options { get; set; } = new();
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

    public class EditableModifierDefinitionDto
    {
        public Guid Id { get; set; }
        public ModifierOperationType OperationType { get; set; }
        public ModifierTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public string TargetNameSnapshot { get; set; } = string.Empty;
        public ModifierValueMode ValueMode { get; set; }
        public int Value { get; set; }
        public Guid? SourceMetricId { get; set; }
    }

    public class EditableTalentDefinitionDto
    {
        public Guid? TalentDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public List<EditableModifierDefinitionDto> Modifiers { get; set; } = new();
    }

    public class EditableItemDefinitionDto
    {
        public Guid? ItemDefinitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }
        public List<EditableModifierDefinitionDto> Modifiers { get; set; } = new();
    }

    public class GameSystemEditorDto
    {
        public Guid GameSystemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestResolutionMode TestResolutionMode { get; set; }
        public int DefaultTestDiceCount { get; set; } = 1;
        public int DefaultTestDiceSides { get; set; } = 100;
        public int? CriticalSuccessValue { get; set; }
        public int? CriticalFailureValue { get; set; }
        public bool IsLockedToSessionCopy { get; set; }
        public bool CanUndoLastChange { get; set; }

        public List<GameSystemImpactSessionDto> ImpactedSessions { get; set; } = new();
        public List<BaseAttributeReferenceDto> AvailableBaseAttributes { get; set; } = new();
        public List<EditableDerivedStatDefinitionDto> AvailableDerivedStats { get; set; } = new();
        public List<EditableMetricDefinitionDto> AvailableMetrics { get; set; } = new();
        public List<EditableAttributeDefinitionDto> Attributes { get; set; } = new();
        public List<EditableDerivedStatDefinitionDto> DerivedStats { get; set; } = new();
        public List<EditableMetricDefinitionDto> Metrics { get; set; } = new();
        public List<EditableTraitDefinitionDto> Traits { get; set; } = new();
        public List<EditableGaugeDefinitionDto> Gauges { get; set; } = new();
        public List<EditableTalentDefinitionDto> Talents { get; set; } = new();
        public List<EditableItemDefinitionDto> Items { get; set; } = new();
    }

    public class GameSystemApplyChangesRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestResolutionMode TestResolutionMode { get; set; }
        public int DefaultTestDiceCount { get; set; } = 1;
        public int DefaultTestDiceSides { get; set; } = 100;
        public int? CriticalSuccessValue { get; set; }
        public int? CriticalFailureValue { get; set; }
        public bool ConfirmSharedSystemChanges { get; set; }

        public List<EditableAttributeDefinitionDto> Attributes { get; set; } = new();
        public List<EditableDerivedStatDefinitionDto> DerivedStats { get; set; } = new();
        public List<EditableMetricDefinitionDto> Metrics { get; set; } = new();
        public List<EditableTraitDefinitionDto> Traits { get; set; } = new();
        public List<EditableGaugeDefinitionDto> Gauges { get; set; } = new();
        public List<EditableTalentDefinitionDto> Talents { get; set; } = new();
        public List<EditableItemDefinitionDto> Items { get; set; } = new();
    }
}
