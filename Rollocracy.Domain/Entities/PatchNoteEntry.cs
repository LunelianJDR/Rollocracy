using System;

namespace Rollocracy.Domain.Entities
{
    public class PatchNoteEntry
    {
        public Guid Id { get; set; }

        public Guid PatchNoteId { get; set; }

        public string TextFr { get; set; } = string.Empty;

        public string TextEn { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public PatchNote? PatchNote { get; set; }
    }
}
