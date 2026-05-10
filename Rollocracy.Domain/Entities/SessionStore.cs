using System;

namespace Rollocracy.Domain.Entities
{
    public class SessionStore
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        public bool IsEnabled { get; set; }
    }
}