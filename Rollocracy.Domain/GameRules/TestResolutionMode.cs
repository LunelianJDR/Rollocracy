namespace Rollocracy.Domain.GameRules
{
    public enum TestResolutionMode
    {
        // Le joueur doit obtenir un résultat >= à une valeur cible
        SuccessThreshold = 0,

        // Le joueur doit obtenir un résultat <= à la caractéristique testée
        RollUnderAttribute = 1
    }
}