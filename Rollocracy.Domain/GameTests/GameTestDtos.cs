using System;
using System.Collections.Generic;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.Characters;

namespace Rollocracy.Domain.GameTests
{

    public enum GameTestAdvancedModifierSourceType
    {
        TraitOption = 0,
        Talent = 1,
        Item = 2
    }

    public enum GameTestAdvancedModifierCombinationMode
    {
        Cumulative = 0,
        HighestOnly = 1
    }

    public enum GameTestSuccessThresholdMode
    {
        Fixed = 0,
        Metric = 1
    }

    public enum TestConsequenceApplicationMode
    {
        ApplyOnce = 0,
        PerCharacter = 1,
        PerSuccessfulCharacter = 2,
        PerFailedCharacter = 3,
        PerEligibleCharacter = 4,
        PerPollOptionVoter = 5
    }

    public enum ConsequenceClampMode
    {
        None = 0,
        ClampTotal = 1
    }

    public class GameTestAdvancedModifierDto
    {
        public GameTestAdvancedModifierSourceType SourceType { get; set; } = GameTestAdvancedModifierSourceType.TraitOption;

        public List<Guid> SelectedIds { get; set; } = new();

        public ModifierValueMode ValueMode { get; set; } = ModifierValueMode.Fixed;

        public int FixedValue { get; set; }

        public TestModifierMode MetricModifierMode { get; set; } = TestModifierMode.Bonus;

        public Guid? SourceMetricId { get; set; }
    }

    public class GameTestTraitFilterGroupDto
    {
        public Guid TraitDefinitionId { get; set; }

        public string TraitDefinitionName { get; set; } = string.Empty;

        public List<Guid> SelectedOptionIds { get; set; } = new();
    }

    public class GameTestConsequenceDraftDto
    {
        public TestConsequenceApplyOn ApplyOn { get; set; }

        public TestConsequenceApplicationMode ApplicationMode { get; set; } = TestConsequenceApplicationMode.ApplyOnce;

        public TestConsequenceOperationType OperationType { get; set; }

        public TestConsequenceTargetKind TargetKind { get; set; }

        public Guid TargetDefinitionId { get; set; }

        public string TargetName { get; set; } = string.Empty;

        // Utilisé uniquement pour AddValue
        // Conservé pour compatibilité technique/transitoire.
        // En L-2, l'UI ne l'expose plus et on force Bonus.
        public TestModifierMode ModifierMode { get; set; } = TestModifierMode.Bonus;

        // Utilisé uniquement pour AddValue
        public int Value { get; set; }

        public ConsequenceClampMode ClampMode { get; set; } = ConsequenceClampMode.None;

        public int ClampMinTotal { get; set; } = -100;

        public int ClampMaxTotal { get; set; } = 100;

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

        public GameTestSuccessThresholdMode SuccessThresholdMode { get; set; } = GameTestSuccessThresholdMode.Fixed;

        public int? SuccessThreshold { get; set; }

        public Guid? SuccessThresholdMetricId { get; set; }

        public TestModifierMode ModifierMode { get; set; }

        public int DifficultyValue { get; set; }

        public TestTargetScope TargetScope { get; set; }

        public TestTraitFilterMode TraitFilterMode { get; set; }

        public bool IncludeNpcs { get; set; }

        public int AutoRollDelaySeconds { get; set; } = 20;

        public int? GlobalSuccessThreshold1Percent { get; set; } = 50;
        public int? GlobalSuccessThreshold2Percent { get; set; }
        public int? GlobalSuccessThreshold3Percent { get; set; }

        public CharacterTargetFilterDto AdvancedFilter { get; set; } = new();

        public List<GameTestTraitFilterGroupDto> TraitFilters { get; set; } = new();

        public bool AdvancedModifiersEnabled { get; set; }

        public GameTestAdvancedModifierCombinationMode AdvancedModifierCombinationMode { get; set; } = GameTestAdvancedModifierCombinationMode.Cumulative;

        public List<GameTestAdvancedModifierDto> AdvancedModifiers { get; set; } = new();

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

        public GameTestSuccessThresholdMode SuccessThresholdMode { get; set; } = GameTestSuccessThresholdMode.Fixed;

        public int? SuccessThreshold { get; set; }

        public Guid? SuccessThresholdMetricId { get; set; }

        public string SuccessThresholdMetricName { get; set; } = string.Empty;

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

        public int? EffectiveSuccessThreshold { get; set; }

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

        public int? EffectiveSuccessThreshold { get; set; }
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

        public GameTestSuccessThresholdMode SuccessThresholdMode { get; set; } = GameTestSuccessThresholdMode.Fixed;

        public int? SuccessThreshold { get; set; }

        public Guid? SuccessThresholdMetricId { get; set; }

        public string SuccessThresholdMetricName { get; set; } = string.Empty;

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

        public int? GlobalSuccessThreshold1Percent { get; set; }
        public int? GlobalSuccessThreshold2Percent { get; set; }
        public int? GlobalSuccessThreshold3Percent { get; set; }
        public GameTestGlobalOutcome GlobalOutcome { get; set; }
        public bool HasConsequences { get; set; }
        public bool ConsequencesCancelled { get; set; }

        public int? BestDiceTotal { get; set; }

        public int? WorstDiceTotal { get; set; }

        public double? AverageDiceTotal { get; set; }

        public List<GameMasterGameTestResultLineDto> Results { get; set; } = new();
    }

    public class SessionGameTestPresetDto
    {
        public Guid PresetId { get; set; }

        public Guid SessionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public GameTestCreateRequestDto Request { get; set; } = new();
    }
}