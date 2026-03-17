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

        Task<Character> CreateNpcAsync(
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
            bool includeDead,
            bool includeNpcs);

        Task<List<SessionCharacterSummaryDto>> GetSessionCharacterSummariesAsync(
            Guid sessionId,
            bool includeOffline,
            bool includeDead);

        Task<List<SessionCharacterSummaryDto>> GetSessionCharacterSummariesForPlayerAsync(
            Guid playerSessionId,
            bool includeOffline,
            bool includeDead,
            bool includeNpcs);

        Task<List<SessionCharacterSummaryDto>> GetSessionCharacterSummariesForPlayerAsync(
            Guid playerSessionId,
            bool includeOffline,
            bool includeDead);

        Task<Guid> GetPlayerSessionIdForUserAsync(Guid sessionId, Guid userAccountId);
        Task<CharacterSheetDto?> GetCharacterSheetForSessionAsync(Guid sessionId, Guid characterId);

        Task<CharacterSheetDto?> GetCharacterSheetForPlayerAsync(Guid playerSessionId, Guid characterId);

        Task<EditableCharacterDto?> GetEditableCharacterForSessionAsync(
            Guid sessionId,
            Guid characterId,
            Guid gameMasterUserAccountId);

        Task<CharacterUpdateResultDto> UpdateCharacterForSessionAsync(
            Guid sessionId,
            Guid characterId,
            Guid gameMasterUserAccountId,
            UpdateCharacterRequestDto request);
    }
}
