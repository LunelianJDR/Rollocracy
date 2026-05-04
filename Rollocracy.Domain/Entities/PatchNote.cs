using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Entities
{
    public class PatchNote
    {
        public Guid Id { get; set; }

        public string Version { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = true;

        public int DisplayOrder { get; set; }

        public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<PatchNoteEntry> Entries { get; set; } = new List<PatchNoteEntry>();
    }
}
