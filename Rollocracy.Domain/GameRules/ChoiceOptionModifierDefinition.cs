using System;

namespace Rollocracy.Domain.GameRules;

public class ChoiceOptionModifierDefinition
{
    public Guid Id { get; set; }

    public Guid ChoiceOptionDefinitionId { get; set; }

    public ModifierOperationType OperationType { get; set; }

    public ModifierTargetType TargetType { get; set; }

    public Guid TargetId { get; set; }

    public string TargetNameSnapshot { get; set; } = string.Empty;

    public ModifierValueMode ValueMode { get; set; }

    public int Value { get; set; }

    public Guid? SourceMetricId { get; set; }
}