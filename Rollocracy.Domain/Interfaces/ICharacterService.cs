using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Interfaces
{
    public interface ICharacterService
    {
        Task<PlayerRoomStateDto> GetPlayerRoomStateAsync(Guid playerSessionId);

        Task<CharacterCreationTemplateDto> GetCharacterCreationTemplateAsync(Guid playerSessionId);

        Task<Character> CreateCharacterAsync(
            Guid playerSessionId,
            string name,
            string biography,
            Dictionary<Guid, int> attributeValues,
            Dictionary<Guid, Guid> traitSelections);

        Task<CharacterSheetDto?> GetCharacterSheetAsync(Guid playerSessionId, Guid characterId);

        Task<SessionPublicStatsDto> GetSessionPublicStatsAsync(Guid sessionId);

        Task<List<SessionCharacterSummaryDto>> GetSessionCharacterSummariesAsync(
            Guid sessionId,
            bool includeOffline,
            bool includeDead);

        Task<CharacterSheetDto?> GetCharacterSheetForSessionAsync(Guid sessionId, Guid characterId);
    }
}