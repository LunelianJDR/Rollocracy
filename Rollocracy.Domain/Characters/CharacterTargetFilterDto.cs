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

        // Nouveau : filtre par vote sur un sondage précis
        public Guid? PollId { get; set; }
        public Guid? PollSelectedOptionId { get; set; }
        public bool FilterOnPollResponse { get; set; }

        // Filtre sur le dernier test de la session
        public bool? MustHaveSucceededLastTest { get; set; }
        public bool FilterOnLastTestResult { get; set; }

        // Nouveau : filtre texte sur nom de personnage ou de joueur
        public List<Guid> NameCharacterIds { get; set; } = new();
    }
}