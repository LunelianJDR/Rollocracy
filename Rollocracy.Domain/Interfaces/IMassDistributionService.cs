using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rollocracy.Domain.Characters;

namespace Rollocracy.Domain.Interfaces
{
    public interface IMassDistributionService
    {
        Task<MassDistributionEditorDto?> GetEditorAsync(Guid sessionId, Guid userAccountId);

        Task<List<MassDistributionPreviewCharacterDto>> PreviewTargetsAsync(
            Guid sessionId,
            Guid userAccountId,
            MassDistributionRequestDto request);

        Task<int> ApplyAsync(
            Guid sessionId,
            Guid userAccountId,
            MassDistributionRequestDto request);

        Task<MassDistributionLastBatchDto?> GetLastBatchAsync(
            Guid sessionId,
            Guid userAccountId);

        Task<string> UndoLastAsync(
            Guid sessionId,
            Guid userAccountId);
    }
}
