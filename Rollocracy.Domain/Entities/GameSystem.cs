using System;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.Entities
{
    public class GameSystem
    {
        public Guid Id { get; set; }

        public Guid OwnerUserAccountId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Définit la logique globale de résolution des tests
        public TestResolutionMode TestResolutionMode { get; set; }

        // Si ce système est une copie, référence vers le système d'origine
        public Guid? SourceGameSystemId { get; set; }

        // Si ce système est réservé à une session précise
        public Guid? LockedToSessionId { get; set; }
    }
}