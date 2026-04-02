using System;
using System.Collections.Generic;

namespace Rollocracy.Domain.Entities
{
    public class SessionJournalPageDto
    {
        public Guid? JournalPageId { get; set; }

        public int PageNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentHtml { get; set; } = string.Empty;

        public bool IsVisible { get; set; }
    }

    public class SessionJournalEditorDto
    {
        public Guid SessionId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public List<SessionJournalPageDto> PrivatePages { get; set; } = new();

        public List<SessionJournalPageDto> PublicPages { get; set; } = new();
    }

    public class SessionJournalSaveRequestDto
    {
        public List<SessionJournalPageDto> PrivatePages { get; set; } = new();

        public List<SessionJournalPageDto> PublicPages { get; set; } = new();
    }

    public class SessionJournalViewDto
    {
        public Guid SessionId { get; set; }

        public string SessionName { get; set; } = string.Empty;

        public List<SessionJournalPageDto> PublicPages { get; set; } = new();
    }
}