using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Domain.Dice
{
    /// Moteur responsable de générer les jets de dés et déterminer leur résultat.
    public static class DiceEngine
    {
        private static readonly Random _random = new();

        /// Effectue un jet de dés basé sur une définition de test.
        public static DiceRoll Roll(
            DiceDefinition definition,
            Guid playerSessionId,
            int attributeModifier = 0)
        {
            var roll = new DiceRoll
            {
                Id = Guid.NewGuid(),
                PlayerSessionId = playerSessionId,
                DiceDefinitionId = definition.Id
            };

            int total = 0;

            // Lancer les dés
            for (int i = 0; i < definition.DiceCount; i++)
            {
                int result = _random.Next(1, definition.DiceSides + 1);

                roll.DiceResults.Add(result);
                total += result;
            }

            // Ajouter les modificateurs
            total += definition.FlatModifier;
            total += attributeModifier;

            roll.Modifier = definition.FlatModifier + attributeModifier;
            roll.Total = total;

            // Déterminer la réussite
            if (definition.ComparisonType == ComparisonType.GreaterOrEqual)
            {
                roll.IsSuccess = total >= definition.TargetValue;
            }
            else
            {
                roll.IsSuccess = total <= definition.TargetValue;
            }

            return roll;
        }
    }
}
