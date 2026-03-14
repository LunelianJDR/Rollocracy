using System;
using System.Collections.Generic;
using Rollocracy.Domain.GameTests;

namespace Rollocracy.Domain.Polls
{
    public class PollWeightRuleDraftDto
    {
        public Guid TraitDefinitionId { get; set; }
        public Guid TraitOptionId { get; set; }
        public decimal WeightBonus { get; set; }
    }

    public class PollOptionConsequenceDraftDto
    {
        public Guid SessionPollOptionId { get; set; }
        public TestConsequenceTargetKind TargetKind { get; set; }
        public Guid TargetDefinitionId { get; set; }
        public TestModifierMode ModifierMode { get; set; }
        public int Value { get; set; }
    }

    public class PollOptionDraftDto
    {
        public string Label { get; set; } = string.Empty;
        public List<PollOptionConsequenceInlineDraftDto> Consequences { get; set; } = new();
    }

    public class PollOptionConsequenceInlineDraftDto
    {
        public TestConsequenceTargetKind TargetKind { get; set; }
        public Guid TargetDefinitionId { get; set; }
        public TestModifierMode ModifierMode { get; set; }
        public int Value { get; set; }
    }

    public class PollCreateRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public List<PollOptionDraftDto> Options { get; set; } = new();

        // Compatibilité transitoire : ancienne mécanique non utilisée par le nouveau flux.
        public List<PollWeightRuleDraftDto> WeightRules { get; set; } = new();

        public PollVoteWeightMode VoteWeightMode { get; set; } = PollVoteWeightMode.FixedOne;
        public Guid? MetricDefinitionId { get; set; }
    }

    public class PollVoteLineDto
    {
        public Guid VoteId { get; set; }
        public Guid PlayerSessionId { get; set; }
        public Guid CharacterId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public Guid OptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public decimal VoteWeight { get; set; }
    }

    public class PollOptionResultDto
    {
        public Guid OptionId { get; set; }
        public string Label { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public decimal WeightedVoteTotal { get; set; }
        public double VotePercent { get; set; }
        public double WeightedVotePercent { get; set; }
    }

    public class PollWeightRuleDto
    {
        public Guid TraitDefinitionId { get; set; }
        public Guid TraitOptionId { get; set; }
        public string TraitDefinitionName { get; set; } = string.Empty;
        public string TraitOptionName { get; set; } = string.Empty;
        public decimal WeightBonus { get; set; }
    }

    public class PollOptionConsequenceDto
    {
        public Guid SessionPollOptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public TestConsequenceTargetKind TargetKind { get; set; }
        public Guid TargetDefinitionId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public TestModifierMode ModifierMode { get; set; }
        public int Value { get; set; }
    }

    public class PollForGameMasterDto
    {
        public Guid PollId { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public bool ConsequencesApplied { get; set; }
        public PollVoteWeightMode VoteWeightMode { get; set; } = PollVoteWeightMode.FixedOne;
        public Guid? MetricDefinitionId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public int TotalVotes { get; set; }
        public decimal TotalWeightedVotes { get; set; }
        public int OnlinePlayersCount { get; set; }
        public double ParticipationPercent { get; set; }
        public List<PollOptionResultDto> Options { get; set; } = new();
        public List<PollVoteLineDto> Votes { get; set; } = new();
        public List<PollWeightRuleDto> WeightRules { get; set; } = new();
        public List<PollOptionConsequenceDto> Consequences { get; set; } = new();
    }

    public class PollForPlayerDto
    {
        public Guid PollId { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool IsClosed { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public bool HasVoted { get; set; }
        public Guid? SelectedOptionId { get; set; }
        public decimal? VoteWeight { get; set; }
        public PollVoteWeightMode VoteWeightMode { get; set; } = PollVoteWeightMode.FixedOne;
        public string MetricName { get; set; } = string.Empty;
        public List<PollOptionResultDto> Options { get; set; } = new();
    }
}
