using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Interfaces
{
    // Interface du moteur commun d'effets sur personnages.
    // 5A.3 ajoute la résolution des cibles à partir des filtres,
    // puis l'application nommée d'un lot complet.
    public interface ICharacterEffectService
    {
        Task ApplyEffectsAsync(
            Guid sessionId,
            List<Guid> characterIds,
            List<CharacterEffectDefinitionDto> effects,
            CharacterEffectSourceType sourceType,
            Guid sourceId,
            string sourceName);

        Task<List<Guid>> ResolveTargetCharacterIdsAsync(
            Guid sessionId,
            CharacterTargetFilterDto filter);

        Task<List<Character>> GetTargetCharactersAsync(
            Guid sessionId,
            CharacterTargetFilterDto filter);

        Task<int> ApplyNamedEffectBatchAsync(
            Guid sessionId,
            string sourceName,
            CharacterEffectSourceType sourceType,
            Guid sourceId,
            CharacterTargetFilterDto filter,
            List<CharacterEffectDefinitionDto> effects);
    }
}
