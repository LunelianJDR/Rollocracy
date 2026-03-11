using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.Interfaces
{
    public interface IGameSystemService
    {
        // Liste les systèmes réutilisables du MJ
        Task<List<GameSystem>> GetGameSystemsByOwnerAsync(Guid ownerUserAccountId);

        // Récupère un système précis
        Task<GameSystem?> GetGameSystemByIdAsync(Guid gameSystemId, Guid ownerUserAccountId);

        // Crée un nouveau système de base
        Task<GameSystem> CreateGameSystemAsync(
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode);

        // Met à jour les infos générales d'un système
        Task UpdateGameSystemAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode);

        // Ajoute une caractéristique numérique
        Task<AttributeDefinition> AddAttributeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue);

        // Liste les caractéristiques d'un système
        Task<List<AttributeDefinition>> GetAttributeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        // Ajoute un type d'attribut à choix
        Task<TraitDefinition> AddTraitDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name);

        // Liste les types d'attributs à choix
        Task<List<TraitDefinition>> GetTraitDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        // Ajoute une option à un attribut à choix
        Task<TraitOption> AddTraitOptionAsync(
            Guid traitDefinitionId,
            Guid ownerUserAccountId,
            string name);

        // Liste les options d'un attribut à choix
        Task<List<TraitOption>> GetTraitOptionsAsync(Guid traitDefinitionId, Guid ownerUserAccountId);

        // Liste les jauges d'un système
        Task<List<GaugeDefinition>> GetGaugeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        // Ajoute une jauge à un système
        Task<GaugeDefinition> AddGaugeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue,
            bool isHealthGauge);

        // Duplique un système pour une session spécifique
        Task<GameSystem> CloneGameSystemForSessionAsync(
            Guid sourceGameSystemId,
            Guid ownerUserAccountId,
            Guid sessionId);
    }
}