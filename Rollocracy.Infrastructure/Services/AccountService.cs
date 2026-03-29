using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Account;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private const int DefaultGameMasterCapacity = 15;
        private const string FreePlanCode = "aventurier";

        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;

        public AccountService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<AccountSettingsDto> GetAccountSettingsAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);

            var user = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var subscription = await context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

            SubscriptionPlan? currentPlan = null;
            SubscriptionPlan? pendingPlan = null;

            if (subscription is not null)
            {
                currentPlan = await context.SubscriptionPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == subscription.CurrentPlanId);

                if (subscription.PendingPlanId.HasValue)
                {
                    pendingPlan = await context.SubscriptionPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == subscription.PendingPlanId.Value);
                }
            }

            return new AccountSettingsDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Language = user.Language,
                IsTwitchLinked = user.IsTwitchLinked,
                TwitchLogin = user.TwitchLogin,
                WantsToBeGameMaster = user.WantsToBeGameMaster,
                MaxPlayersPerSession = user.MaxPlayersPerSession,
                CreatedAt = user.CreatedAt,
                LastSensitiveChangeAtUtc = user.LastSensitiveChangeAtUtc,
                LastSensitiveChangeType = user.LastSensitiveChangeType,

                CurrentSubscriptionPlanId = currentPlan?.Id,
                CurrentSubscriptionPlanCode = currentPlan?.Code,
                SubscriptionStartedAtUtc = subscription?.StartedAtUtc,
                NextSubscriptionRenewalAtUtc = subscription?.NextRenewalAtUtc,
                PendingSubscriptionPlanId = pendingPlan?.Id,
                PendingSubscriptionPlanCode = pendingPlan?.Code,
                CancelSubscriptionAtRenewal = subscription?.CancelAtRenewal ?? false
            };
        }

        public async Task UpdateGeneralInfoAsync(
            Guid userAccountId,
            string? email,
            string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var normalizedEmail = NormalizeEmail(email);
            var normalizedLanguage = NormalizeLanguage(language);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var existingUserWithEmail = await context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Id != userAccountId);

                if (existingUserWithEmail != null)
                    throw new Exception(_localizer["Backend_EmailAlreadyExists"]);
            }

            var emailChanged = user.Email != normalizedEmail;
            var languageChanged = user.Language != normalizedLanguage;

            user.Email = normalizedEmail;
            user.Language = normalizedLanguage;

            if (emailChanged)
            {
                user.IsEmailVerified = false;
                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_EmailChanged";
            }

            // Si l’utilisateur enlève son email, le mode MJ produit est désactivé
            // et la capacité effective redevient 0.
            if (user.WantsToBeGameMaster && string.IsNullOrWhiteSpace(user.Email))
            {
                user.WantsToBeGameMaster = false;
                user.MaxPlayersPerSession = 0;
                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_GameMasterModeDisabled";
            }

            if (emailChanged || languageChanged)
                await context.SaveChangesAsync();
        }

        public async Task UpdateUsernameAsync(Guid userAccountId, string newUsername)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_UsernameChangeRequiresEmail"]);

            var normalizedUsername = NormalizeUsername(newUsername);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
                throw new Exception(_localizer["Backend_UsernameRequired"]);

            if (string.Equals(user.Username, normalizedUsername, StringComparison.Ordinal))
                return;

            var existingUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername && u.Id != userAccountId);

            if (existingUser != null)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            user.Username = normalizedUsername;
            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_UsernameChanged";

            await context.SaveChangesAsync();
        }

        public async Task SetGameMasterModeAsync(Guid userAccountId, bool enabled)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);

            if (enabled)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new Exception(_localizer["Backend_GameMasterModeRequiresEmail"]);

                if (!user.WantsToBeGameMaster)
                {
                    user.WantsToBeGameMaster = true;

                    var subscription = await context.UserSubscriptions
                        .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

                    if (subscription == null)
                    {
                        var freePlan = await GetFreePlanAsync(context);

                        subscription = new UserSubscription
                        {
                            Id = Guid.NewGuid(),
                            UserAccountId = user.Id,
                            CurrentPlanId = freePlan.Id,
                            StartedAtUtc = DateTime.UtcNow,
                            NextRenewalAtUtc = DateTime.UtcNow.AddMonths(1),
                            CancelAtRenewal = false,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        };

                        context.UserSubscriptions.Add(subscription);

                        user.MaxPlayersPerSession = freePlan.MaxPlayersPerSession;
                    }
                    else
                    {
                        var currentPlan = await context.SubscriptionPlans
                            .FirstOrDefaultAsync(x => x.Id == subscription.CurrentPlanId);

                        user.MaxPlayersPerSession = currentPlan?.MaxPlayersPerSession ?? DefaultGameMasterCapacity;
                    }

                    user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                    user.LastSensitiveChangeType = "Account_SecurityHistory_GameMasterModeEnabled";
                }
            }
            else
            {
                if (user.WantsToBeGameMaster)
                {
                    user.WantsToBeGameMaster = false;
                    user.MaxPlayersPerSession = 0;
                    user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                    user.LastSensitiveChangeType = "Account_SecurityHistory_GameMasterModeDisabled";
                }
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<AccountSubscriptionPlanDto>> GetSubscriptionPlansAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);

            var subscription = await context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

            var plans = await context.SubscriptionPlans
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return plans.Select(plan => new AccountSubscriptionPlanDto
            {
                PlanId = plan.Id,
                Code = plan.Code,
                Name = plan.Name,
                MonthlyPriceTtc = plan.MonthlyPriceTtc,
                MaxPlayersPerSession = plan.MaxPlayersPerSession,
                DisplayOrder = plan.DisplayOrder,
                IsCurrent = subscription?.CurrentPlanId == plan.Id,
                IsPending = subscription?.PendingPlanId == plan.Id
            }).ToList();
        }

        public async Task ChangeSubscriptionPlanAsync(Guid userAccountId, Guid planId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception(_localizer["Backend_SubscriptionRequiresEmail"]);

            if (!user.WantsToBeGameMaster)
                throw new Exception(_localizer["Backend_SubscriptionRequiresGameMasterMode"]);

            var targetPlan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(x => x.Id == planId && x.IsActive);

            if (targetPlan == null)
                throw new Exception(_localizer["Backend_SubscriptionPlanNotFound"]);

            var subscription = await context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

            if (subscription == null)
            {
                subscription = new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    UserAccountId = user.Id,
                    CurrentPlanId = targetPlan.Id,
                    StartedAtUtc = DateTime.UtcNow,
                    NextRenewalAtUtc = DateTime.UtcNow.AddMonths(1),
                    CancelAtRenewal = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                context.UserSubscriptions.Add(subscription);

                user.MaxPlayersPerSession = targetPlan.MaxPlayersPerSession;
                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_SubscriptionChanged";

                await context.SaveChangesAsync();
                return;
            }

            var currentPlan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(x => x.Id == subscription.CurrentPlanId);

            if (currentPlan == null)
                throw new Exception(_localizer["Backend_SubscriptionPlanNotFound"]);

            // Même offre : on annule un éventuel changement programmé.
            if (subscription.CurrentPlanId == targetPlan.Id)
            {
                subscription.PendingPlanId = null;
                subscription.CancelAtRenewal = false;
                subscription.UpdatedAtUtc = DateTime.UtcNow;

                user.MaxPlayersPerSession = user.WantsToBeGameMaster
                    ? currentPlan.MaxPlayersPerSession
                    : 0;

                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_SubscriptionChanged";

                await context.SaveChangesAsync();
                return;
            }

            // Upgrade immédiat
            if (targetPlan.MaxPlayersPerSession > currentPlan.MaxPlayersPerSession)
            {
                subscription.CurrentPlanId = targetPlan.Id;
                subscription.PendingPlanId = null;
                subscription.CancelAtRenewal = false;
                subscription.StartedAtUtc = DateTime.UtcNow;
                subscription.NextRenewalAtUtc = DateTime.UtcNow.AddMonths(1);
                subscription.UpdatedAtUtc = DateTime.UtcNow;

                user.MaxPlayersPerSession = user.WantsToBeGameMaster
                    ? targetPlan.MaxPlayersPerSession
                    : 0;
            }
            else
            {
                // Downgrade programmé au renouvellement
                subscription.PendingPlanId = targetPlan.Id;
                subscription.CancelAtRenewal = false;
                subscription.UpdatedAtUtc = DateTime.UtcNow;

                user.MaxPlayersPerSession = user.WantsToBeGameMaster
                    ? currentPlan.MaxPlayersPerSession
                    : 0;
            }

            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_SubscriptionChanged";

            await context.SaveChangesAsync();
        }

        public async Task CancelSubscriptionAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == userAccountId);

            if (user == null)
                throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);

            var subscription = await context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

            if (subscription == null)
                return;

            var currentPlan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(x => x.Id == subscription.CurrentPlanId);

            if (currentPlan == null)
                throw new Exception(_localizer["Backend_SubscriptionPlanNotFound"]);

            // Si on est déjà sur le gratuit, rien à faire.
            if (string.Equals(currentPlan.Code, FreePlanCode, StringComparison.OrdinalIgnoreCase))
                return;

            var freePlan = await GetFreePlanAsync(context);

            subscription.PendingPlanId = freePlan.Id;
            subscription.CancelAtRenewal = true;
            subscription.UpdatedAtUtc = DateTime.UtcNow;

            user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
            user.LastSensitiveChangeType = "Account_SecurityHistory_SubscriptionChanged";

            await context.SaveChangesAsync();
        }

        public async Task ApplyDueSubscriptionChangesAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await ApplyDueSubscriptionChangesCoreAsync(context, userAccountId);
        }

        private async Task ApplyDueSubscriptionChangesCoreAsync(RollocracyDbContext context, Guid userAccountId)
        {
            var user = await context.UserAccounts
                .FirstOrDefaultAsync(x => x.Id == userAccountId);

            if (user == null)
                return;

            var subscription = await context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserAccountId == userAccountId);

            if (subscription == null)
                return;

            var hasChanges = false;
            var now = DateTime.UtcNow;

            // On rattrape les renouvellements passés.
            while (subscription.NextRenewalAtUtc <= now)
            {
                if (subscription.CancelAtRenewal)
                {
                    var freePlan = await GetFreePlanAsync(context);

                    subscription.CurrentPlanId = freePlan.Id;
                    subscription.PendingPlanId = null;
                    subscription.CancelAtRenewal = false;
                    subscription.StartedAtUtc = subscription.NextRenewalAtUtc;
                    subscription.NextRenewalAtUtc = subscription.NextRenewalAtUtc.AddMonths(1);
                    subscription.UpdatedAtUtc = now;

                    hasChanges = true;
                }
                else if (subscription.PendingPlanId.HasValue)
                {
                    subscription.CurrentPlanId = subscription.PendingPlanId.Value;
                    subscription.PendingPlanId = null;
                    subscription.StartedAtUtc = subscription.NextRenewalAtUtc;
                    subscription.NextRenewalAtUtc = subscription.NextRenewalAtUtc.AddMonths(1);
                    subscription.UpdatedAtUtc = now;

                    hasChanges = true;
                }
                else
                {
                    subscription.StartedAtUtc = subscription.NextRenewalAtUtc;
                    subscription.NextRenewalAtUtc = subscription.NextRenewalAtUtc.AddMonths(1);
                    subscription.UpdatedAtUtc = now;

                    hasChanges = true;
                }
            }

            var currentPlan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(x => x.Id == subscription.CurrentPlanId);

            if (currentPlan != null)
            {
                var expectedCapacity = user.WantsToBeGameMaster
                    ? currentPlan.MaxPlayersPerSession
                    : 0;

                if (user.MaxPlayersPerSession != expectedCapacity)
                {
                    user.MaxPlayersPerSession = expectedCapacity;
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await context.SaveChangesAsync();
        }

        private async Task<SubscriptionPlan> GetFreePlanAsync(RollocracyDbContext context)
        {
            var freePlan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(x => x.Code == FreePlanCode && x.IsActive);

            if (freePlan == null)
                throw new Exception(_localizer["Backend_FreeSubscriptionPlanNotFound"]);

            return freePlan;
        }

        private static string NormalizeUsername(string username)
        {
            return username?.Trim() ?? string.Empty;
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return email.Trim().ToLowerInvariant();
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "fr";

            var normalized = language.Trim().ToLowerInvariant();

            return normalized switch
            {
                "fr" => "fr",
                "en" => "en",
                _ => "fr"
            };
        }
    }
}