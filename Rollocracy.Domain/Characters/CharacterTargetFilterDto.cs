using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Characters
{
    // Filtre commun de ciblage. Certains champs sont préparés pour les prochaines livraisons.
    public class CharacterTargetFilterDto
    {
        public bool OnlyAlive { get; set; }

        public bool OnlyDead { get; set; }

        public bool OnlyOnline { get; set; }

        public List<Guid> TraitOptionIds { get; set; } = new();

        public List<Guid> TalentIds { get; set; } = new();

        public List<Guid> ItemIds { get; set; } = new();

        public List<CharacterValueFilterDto> ValueFilters { get; set; } = new();

        // Préparation pour 5B : filtrage sur le dernier sondage.
        public Guid? LastPollSelectedOptionId { get; set; }

        public bool FilterOnLastPollResponse { get; set; }

        // Préparation pour 5B : filtrage sur la réussite / l'échec au dernier test.
        public bool? MustHaveSucceededLastTest { get; set; }

        public bool FilterOnLastTestResult { get; set; }
    }
}
