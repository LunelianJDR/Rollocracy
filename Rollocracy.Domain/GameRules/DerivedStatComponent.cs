using System;

namespace Rollocracy.Domain.GameRules
{
    public class DerivedStatComponent
    {
        public Guid Id { get; set; }

        public Guid DerivedStatDefinitionId { get; set; }

        public Guid AttributeDefinitionId { get; set; }

        // Représente un coefficient en pourcentage.
        // Exemples : 50 = 0,5 ; 100 = 1 ; 150 = 1,5.
        public int Weight { get; set; } = 100;
    }
}
