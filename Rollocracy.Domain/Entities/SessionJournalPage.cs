using System;

namespace Rollocracy.Domain.Entities
{
    public class SessionJournalPage
    {
        public Guid Id { get; set; }

        public Guid SessionId { get; set; }

        // false = journal privé, true = journal public
        public bool IsPublic { get; set; }

        public int PageNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentHtml { get; set; } = string.Empty;

        // Utilisé uniquement pour le journal privé côté MJ.
        // Pour le journal public, on force toujours true au save.
        public bool IsVisible { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}