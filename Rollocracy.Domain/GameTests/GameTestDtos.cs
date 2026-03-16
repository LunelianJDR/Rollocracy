using System;
using System.Collections.Generic;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.GameTests
{
    public class GameTestTraitFilterGroupDto
    {
        public Guid TraitDefinitionId { get; set; }

        public string TraitDefinitionName { get; set; } = string.Empty;

        public List<Guid> SelectedOptionIds { get; set; } = new();
    }

    public class GameTestConsequenceDraftDto
    {
        public TestConsequenceApplyOn ApplyOn { get; set; }

        public TestConsequenceOperationType OperationType { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public string TargetName { get; set; } = string.Empty;

        // Utilisé uniquement pour AddValue
        public TestModifierMode ModifierMode { get; set; }

        // Utilisé uniquement pour AddValue
        public int Value { get; set; }

        // 6D : valeur fixe ou metric source
        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;

        // 6D : metric utilisée quand ValueMode = Metric
        public Guid? SourceMetricId { get; set; }
    }

    public class GameTestCreateRequestDto
    {
        public GameTestTargetKind TargetKind { get; set; } = GameTestTargetKind.BaseAttribute;

        public Guid TargetDefinitionId { get; set; }

        public bool UseSystemDefaultDice { get; set; } = true;

        public int DiceCount { get; set; }

        public int DiceSides { get; set; }

        public int? CriticalSuccessValue { get; set; }

        public int? CriticalFailureValue { get; set; }

        public int? SuccessThreshold { get; set; }

        public TestModifierMode ModifierMode { get; set; }

        public int DifficultyValue { get; set; }

        public TestTargetScope TargetScope { get; set; }

        public TestTraitFilterMode TraitFilterMode { get; set; }

        public List<GameTestTraitFilterGroupDto> TraitFilters { get; set; } = new();

        public List<GameTestConsequenceDraftDto> Consequences { get; set; } = new();
    }

    public class ActivePlayerGameTestDto
    {
        public Guid GameTestId { get; set; }

        public GameTestTargetKind TargetKind { get; set; }

        public string AttributeName { get; set; } = string.Empty;

        public TestResolutionMode ResolutionMode { get; set; }

        public bool UseSystemDefaultDice { get; set; }

        public int DiceCount { get; set; }

        public int DiceSides { get; set; }

        public int? CriticalSuccessValue { get; set; }

        public int? CriticalFailureValue { get; set; }

        public int? SuccessThreshold { get; set; }

        public TestModifierMode ModifierMode { get; set; }

        public int DifficultyValue { get; set; }

        public DateTime AutoRollAtUtc { get; set; }

        public bool AlreadyRolled { get; set; }

        public PlayerGameTestResultDto? Result { get; set; }
    }

    public class PlayerGameTestResultDto
    {
        public List<int> DiceResults { get; set; } = new();

        public int DiceTotal { get; set; }

        public int AttributeValue { get; set; }

        public int EffectiveAttributeValue { get; set; }

        public int FinalValue { get; set; }

        public bool IsSuccess { get; set; }

        public GameTestOutcome Outcome { get; set; }

        public bool IsAutoRolled { get; set; }
    }

    public class GameMasterGameTestResultLineDto
    {
        public Guid CharacterId { get; set; }

        public string CharacterName { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public bool HasRolled { get; set; }

        public bool IsSuccess { get; set; }

        public GameTestOutcome Outcome { get; set; }

        public bool IsAutoRolled { get; set; }

        public List<int> DiceResults { get; set; } = new();

        public int DiceTotal { get; set; }

        public int AttributeValue { get; set; }

        public int EffectiveAttributeValue { get; set; }

        public int FinalValue { get; set; }
    }

    public class GameMasterActiveGameTestDto
    {
        public Guid GameTestId { get; set; }

        public GameTestTargetKind TargetKind { get; set; }

        public string AttributeName { get; set; } = string.Empty;

        public TestResolutionMode ResolutionMode { get; set; }

        public bool UseSystemDefaultDice { get; set; }

        public int DiceCount { get; set; }

        public int DiceSides { get; set; }

        public int? CriticalSuccessValue { get; set; }

        public int? CriticalFailureValue { get; set; }

        public int? SuccessThreshold { get; set; }

        public TestModifierMode ModifierMode { get; set; }

        public int DifficultyValue { get; set; }

        public TestTargetScope TargetScope { get; set; }

        public TestTraitFilterMode TraitFilterMode { get; set; }

        public bool IsClosed { get; set; }

        public DateTime AutoRollAtUtc { get; set; }

        public int TargetCount { get; set; }

        public int RolledCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public int CriticalSuccessCount { get; set; }

        public int CriticalFailureCount { get; set; }

        public double SuccessRatePercent { get; set; }

        public int? BestDiceTotal { get; set; }

        public int? WorstDiceTotal { get; set; }

        public double? AverageDiceTotal { get; set; }

        public List<GameMasterGameTestResultLineDto> Results { get; set; } = new();
    }
}