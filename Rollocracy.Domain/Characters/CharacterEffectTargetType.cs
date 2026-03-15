namespace Rollocracy.Domain.Characters
{
    // Cible métier d'un effet appliqué à un personnage.
    public enum CharacterEffectTargetType
    {
        BaseAttribute = 0,
        Gauge = 1,
        DerivedStat = 2,
        Metric = 3,
        Talent = 4,
        Item = 5
    }
}
