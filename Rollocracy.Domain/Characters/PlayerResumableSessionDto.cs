using System;
using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Characters
{
    public class PlayerResumableSessionDto
    {
        public Guid PlayerSessionId { get; set; }
        public Guid SessionId { get; set; }

        public string SessionName { get; set; } = string.Empty;
        public string SessionSlug { get; set; } = string.Empty;
        public string GameMasterUsername { get; set; } = string.Empty;

        public bool SessionIsActive { get; set; }

        public int TotalCharactersCount { get; set; }
        public int AliveCharactersCount { get; set; }
        public int DeadCharactersCount { get; set; }

        public SessionSpecialRole SpecialRole { get; set; }
    }
}