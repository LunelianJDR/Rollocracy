using System;

namespace Rollocracy.Domain.GameRules
{
    public class AttributeDefinition
    {
        public Guid Id { get; set; }
        public Guid GameSystemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int DefaultValue { get; set; }
        public int CreationDistributionPoints { get; set; }
        public int MaxCreationDistributionPerCharacter { get; set; }
        public BaseValueGenerationMode DefaultValueMode { get; set; } = BaseValueGenerationMode.Fixed;
        public int DefaultValueDiceCount { get; set; } = 1;
        public int DefaultValueDiceSides { get; set; } = 6;
        public int DefaultValueFlatBonus { get; set; }
    }
}