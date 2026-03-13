using System;

namespace Rollocracy.Domain.GameRules
{
    /// <summary>
    /// Représente une caractéristique définie dans un système de jeu.
    /// Exemple : Force, Agilité, Intelligence.
    /// </summary>
    public class AttributeDefinition
    {
        /// <summary>
        /// Identifiant unique de la caractéristique.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Système de jeu auquel appartient cette caractéristique.
        /// </summary>
        public Guid GameSystemId { get; set; }

        /// <summary>
        /// Nom de la caractéristique.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Valeur minimale autorisée.
        /// </summary>
        public int MinValue { get; set; }

        /// <summary>
        /// Valeur maximale autorisée.
        /// </summary>
        public int MaxValue { get; set; }

        /// <summary>
        /// Valeur par défaut fixe, utilisée si DefaultValueMode = Fixed.
        /// </summary>
        public int DefaultValue { get; set; }

        /// <summary>
        /// Mode de génération de la valeur initiale.
        /// </summary>
        public BaseValueGenerationMode DefaultValueMode { get; set; } = BaseValueGenerationMode.Fixed;

        /// <summary>
        /// Nombre de dés à lancer si DefaultValueMode = DiceExpression.
        /// </summary>
        public int DefaultValueDiceCount { get; set; } = 1;

        /// <summary>
        /// Nombre de faces des dés si DefaultValueMode = DiceExpression.
        /// </summary>
        public int DefaultValueDiceSides { get; set; } = 6;

        /// <summary>
        /// Bonus fixe ajouté au résultat des dés si DefaultValueMode = DiceExpression.
        /// Exemple : 20 + 2d10 => FlatBonus = 20.
        /// </summary>
        public int DefaultValueFlatBonus { get; set; }
    }
}
