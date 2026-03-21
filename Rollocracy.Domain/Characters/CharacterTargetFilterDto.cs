using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Characters
{
    // Filtre commun de ciblage.
    public class CharacterTargetFilterDto
    {
        public bool OnlyAlive { get; set; }

        public bool OnlyDead { get; set; }

        public bool OnlyOnline { get; set; }

        public bool IncludeNpcs { get; set; } = true;

        // false = au moins une condition
        // true = toutes les conditions
        public bool MatchAllConditions { get; set; } = true;

        public List<Guid> TraitOptionIds { get; set; } = new();

        public List<Guid> TalentIds { get; set; } = new();

        public List<Guid> ItemIds { get; set; } = new();

        public List<CharacterValueFilterDto> ValueFilters { get; set; } = new();

        public Guid? LastPollSelectedOptionId { get; set; }

        public bool FilterOnLastPollResponse { get; set; }

        public bool? MustHaveSucceededLastTest { get; set; }

        public bool FilterOnLastTestResult { get; set; }
    }
}