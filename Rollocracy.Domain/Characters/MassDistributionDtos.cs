using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Characters
{
    public class NamedReferenceDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class MassDistributionPreviewCharacterDto
    {
        public Guid CharacterId { get; set; }

        public string CharacterName { get; set; } = string.Empty;

        public bool IsAlive { get; set; }
    }

    public class MassDistributionLastBatchDto
    {
        public Guid BatchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int TargetCharacterCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class MassDistributionEditorDto
    {
        public Guid SessionId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public List<NamedReferenceDto> TraitOptions { get; set; } = new();

        public List<NamedReferenceDto> Talents { get; set; } = new();

        public List<NamedReferenceDto> Items { get; set; } = new();

        public List<NamedReferenceDto> BaseAttributes { get; set; } = new();

        public List<NamedReferenceDto> Gauges { get; set; } = new();

        public List<NamedReferenceDto> DerivedStats { get; set; } = new();

        public List<NamedReferenceDto> Metrics { get; set; } = new();
    }

    public class MassDistributionRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public CharacterTargetFilterDto Filter { get; set; } = new();

        public List<CharacterEffectDefinitionDto> Effects { get; set; } = new();
    }
}
