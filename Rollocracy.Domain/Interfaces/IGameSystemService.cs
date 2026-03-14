using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;

namespace Rollocracy.Domain.Interfaces
{
    public interface IGameSystemService
    {
        Task<List<GameSystem>> GetGameSystemsByOwnerAsync(Guid ownerUserAccountId);

        Task<GameSystem?> GetGameSystemByIdAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task<GameSystem> CreateGameSystemAsync(
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode);

        Task UpdateGameSystemAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            string description,
            TestResolutionMode testResolutionMode);

        Task<AttributeDefinition> AddAttributeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue);

        Task<List<AttributeDefinition>> GetAttributeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task<List<MetricDefinition>> GetMetricDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task<TraitDefinition> AddTraitDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name);

        Task<List<TraitDefinition>> GetTraitDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task<TraitOption> AddTraitOptionAsync(
            Guid traitDefinitionId,
            Guid ownerUserAccountId,
            string name);

        Task<List<TraitOption>> GetTraitOptionsAsync(Guid traitDefinitionId, Guid ownerUserAccountId);

        Task<List<GaugeDefinition>> GetGaugeDefinitionsAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task<GaugeDefinition> AddGaugeDefinitionAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            string name,
            int minValue,
            int maxValue,
            int defaultValue,
            bool isHealthGauge);

        Task<GameSystem> CloneGameSystemForSessionAsync(
            Guid sourceGameSystemId,
            Guid ownerUserAccountId,
            Guid sessionId);

        Task<GameSystemEditorDto?> GetGameSystemEditorAsync(Guid gameSystemId, Guid ownerUserAccountId);

        Task ApplyGameSystemChangesAsync(
            Guid gameSystemId,
            Guid ownerUserAccountId,
            GameSystemApplyChangesRequestDto request);

        Task UndoLastGameSystemChangeAsync(Guid gameSystemId, Guid ownerUserAccountId);
    }
}
