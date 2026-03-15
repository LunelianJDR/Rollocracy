namespace Rollocracy.Domain.Characters
{
    // Opération demandée par un effet commun.
    public enum CharacterEffectOperationType
    {
        AddValue = 0,
        GrantTalent = 1,
        RevokeTalent = 2,
        GrantItem = 3,
        RevokeItem = 4
    }
}
