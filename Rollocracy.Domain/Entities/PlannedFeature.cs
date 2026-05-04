using System;

namespace Rollocracy.Domain.Entities
{
    public class PlannedFeature
    {
        public Guid Id { get; set; }

        public string TextFr { get; set; } = string.Empty;

        public string TextEn { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
