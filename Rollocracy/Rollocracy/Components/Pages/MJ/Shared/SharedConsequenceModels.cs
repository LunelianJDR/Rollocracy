using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;

namespace Rollocracy.Components.Pages.MJ.Shared
{
    public class SharedConsequenceRowModel
    {
        public Guid RowId { get; set; } = Guid.NewGuid();
        public TestConsequenceApplyOn ApplyOn { get; set; } = TestConsequenceApplyOn.OnSuccess;
        public TestConsequenceApplicationMode ApplicationMode { get; set; } = TestConsequenceApplicationMode.ApplyOnce;
        public TestConsequenceOperationType OperationType { get; set; } = TestConsequenceOperationType.AddValue;
        public TestConsequenceTargetKind TargetKind { get; set; } = TestConsequenceTargetKind.Attribute;
        public Guid TargetDefinitionId { get; set; }
        public TestModifierMode ModifierMode { get; set; } = TestModifierMode.Bonus;
        public int Value { get; set; } = 1;
        public ConsequenceClampMode ClampMode { get; set; } = ConsequenceClampMode.None;
        public int ClampMinTotal { get; set; } = -100;
        public int ClampMaxTotal { get; set; } = 100;
        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;
        public Guid? SourceMetricId { get; set; }
    }

    public class SharedConsequenceApplyOnOption
    {
        public TestConsequenceApplyOn Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class SharedConsequenceDefinitionOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}