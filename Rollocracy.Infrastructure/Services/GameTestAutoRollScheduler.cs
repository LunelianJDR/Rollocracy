using Microsoft.Extensions.DependencyInjection;
using Rollocracy.Domain.Interfaces;

namespace Rollocracy.Infrastructure.Services
{
    public class GameTestAutoRollScheduler
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public GameTestAutoRollScheduler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void ScheduleAutoRoll(Guid gameTestId, TimeSpan delay)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay);

                    using var scope = _scopeFactory.CreateScope();
                    var gameTestService = scope.ServiceProvider.GetRequiredService<IGameTestService>();

                    await gameTestService.AutoRollPendingAsync(gameTestId);
                }
                catch
                {
                    // V1 : on ignore silencieusement l'erreur de fond
                }
            });
        }
    }
}