using System;

namespace Rollocracy.Domain.Polls
{
    public class SessionPollOption
    {
        public Guid Id { get; set; }

        public Guid SessionPollId { get; set; }

        public string Label { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}