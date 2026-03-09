using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.GameRules
{
    public class CharacterAttributeValue
    {
        /// Identifiant unique
        public Guid Id { get; set; }

        /// Personnage concerné
        public Guid CharacterId { get; set; }

        /// Attribut concerné (Force, Agilité, etc.)
        public Guid AttributeDefinitionId { get; set; }

        /// Valeur actuelle de l'attribut
        public int Value { get; set; }
    }
}
