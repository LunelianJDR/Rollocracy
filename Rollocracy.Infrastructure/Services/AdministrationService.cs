using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Admin;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AdministrationService : IAdministrationService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IAccountSecurityService _accountSecurityService;
        private readonly IStringLocalizer _localizer;

        public AdministrationService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IAccountSecurityService accountSecurityService,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;
            _accountSecurityService = accountSecurityService;
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        public async Task<bool> IsAdministratorAsync(Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await IsAdministratorCoreAsync(context, userAccountId);
        }

        public async Task<AdminDashboardDto> GetDashboardAsync(Guid administratorUserAccountId, string baseUri)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var normalizedBaseUri = string.IsNullOrWhiteSpace(baseUri)
                ? "/"
                : baseUri.TrimEnd('/') + "/";

            var latestUser = await context.UserAccounts
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            var playerSessionRows = await context.PlayerSessions
                .AsNoTracking()
                .ToListAsync();

            var characterRows = await context.Characters
                .AsNoTracking()
                .ToListAsync();

            var subscriptions = await context.UserSubscriptions
                .AsNoTracking()
                .ToListAsync();

            var subscriptionPlans = await context.SubscriptionPlans
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Code)
                .ToListAsync();

            var users = await context.UserAccounts
                .AsNoTracking()
                .OrderBy(x => x.Username)
                .ToListAsync();

            var gameSystems = await context.GameSystems
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            var sessions = await context.Sessions
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new AdminDashboardDto
            {
                UserCount = users.Count,
                GameMasterAccountCount = users.Count(x => x.IsGameMaster || x.WantsToBeGameMaster || x.MaxPlayersPerSession > 0),
                ActiveSessionCount = sessions.Count(x => x.IsActive),
                SessionCount = sessions.Count,
                GameSystemCount = gameSystems.Count,
                CharacterCount = characterRows.Count,
                AliveCharacterCount = characterRows.Count(x => x.IsAlive),
                LatestUserName = latestUser?.Username ?? string.Empty,
                LatestUserCreatedAtUtc = latestUser?.CreatedAt,
                PatchNotes = await BuildAdminPatchNotesAsync(context),
                PlannedFeatures = await BuildAdminPlannedFeaturesAsync(context),
                Users = BuildAdminUsers(users, subscriptions, subscriptionPlans),
                SubscriptionPlans = BuildAdminSubscriptionPlans(subscriptionPlans, subscriptions),
                GameSystems = BuildAdminGameSystems(gameSystems, users, sessions),
                Sessions = BuildAdminSessions(sessions, users, gameSystems, playerSessionRows, characterRows, normalizedBaseUri)
            };
        }

        public async Task<HomeProjectContentDto> GetHomeProjectContentAsync(string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var normalizedLanguage = NormalizeLanguage(language);

            var patchNotes = await context.PatchNotes
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.PublishedAtUtc)
                .Take(8)
                .ToListAsync();

            var patchNoteIds = patchNotes.Select(x => x.Id).ToList();

            var entries = await context.PatchNoteEntries
                .AsNoTracking()
                .Where(x => patchNoteIds.Contains(x.PatchNoteId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var plannedFeatures = await context.PlannedFeatures
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedAtUtc)
                .ToListAsync();

            return new HomeProjectContentDto
            {
                PatchNotes = patchNotes.Select(note => new PublicPatchNoteDto
                {
                    Id = note.Id,
                    Version = note.Version,
                    PublishedAtUtc = note.PublishedAtUtc,
                    Entries = entries
                        .Where(entry => entry.PatchNoteId == note.Id)
                        .Select(entry => GetLocalizedText(entry.TextFr, entry.TextEn, normalizedLanguage))
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .ToList()
                }).ToList(),
                PlannedFeatures = plannedFeatures
                    .Select(feature => new PublicPlannedFeatureDto
                    {
                        Id = feature.Id,
                        Text = GetLocalizedText(feature.TextFr, feature.TextEn, normalizedLanguage)
                    })
                    .Where(feature => !string.IsNullOrWhiteSpace(feature.Text))
                    .ToList()
            };
        }

        public async Task<AdminPatchNoteDto> SavePatchNoteAsync(Guid administratorUserAccountId, AdminPatchNoteSaveDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var version = (request.Version ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(version))
                throw new Exception(_localizer["Admin_PatchNoteVersionRequired"]);

            var existingWithVersion = await context.PatchNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Version == version && (!request.Id.HasValue || x.Id != request.Id.Value));

            if (existingWithVersion is not null)
                throw new Exception(_localizer["Admin_PatchNoteVersionAlreadyExists"]);

            PatchNote patchNote;
            if (request.Id.HasValue)
            {
                patchNote = await context.PatchNotes
                    .FirstOrDefaultAsync(x => x.Id == request.Id.Value)
                    ?? throw new Exception(_localizer["Admin_PatchNoteNotFound"]);
            }
            else
            {
                patchNote = new PatchNote
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.PatchNotes.Add(patchNote);
            }

            patchNote.Version = version;
            patchNote.IsPublished = request.IsPublished;
            patchNote.DisplayOrder = request.DisplayOrder;
            patchNote.PublishedAtUtc = request.PublishedAtUtc ?? DateTime.UtcNow;
            patchNote.UpdatedAtUtc = DateTime.UtcNow;

            var existingEntries = await context.PatchNoteEntries
                .Where(x => x.PatchNoteId == patchNote.Id)
                .ToListAsync();

            context.PatchNoteEntries.RemoveRange(existingEntries);

            var order = 0;
            foreach (var entry in request.Entries.Where(IsMeaningfulPatchNoteEntry))
            {
                context.PatchNoteEntries.Add(new PatchNoteEntry
                {
                    Id = Guid.NewGuid(),
                    PatchNoteId = patchNote.Id,
                    TextFr = entry.TextFr.Trim(),
                    TextEn = entry.TextEn.Trim(),
                    DisplayOrder = order++
                });
            }

            await context.SaveChangesAsync();

            return (await BuildAdminPatchNotesAsync(context)).First(x => x.Id == patchNote.Id);
        }

        public async Task DeletePatchNoteAsync(Guid administratorUserAccountId, Guid patchNoteId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var patchNote = await context.PatchNotes.FirstOrDefaultAsync(x => x.Id == patchNoteId)
                ?? throw new Exception(_localizer["Admin_PatchNoteNotFound"]);

            var entries = await context.PatchNoteEntries.Where(x => x.PatchNoteId == patchNoteId).ToListAsync();
            context.PatchNoteEntries.RemoveRange(entries);
            context.PatchNotes.Remove(patchNote);
            await context.SaveChangesAsync();
        }

        public async Task<AdminPlannedFeatureDto> SavePlannedFeatureAsync(Guid administratorUserAccountId, AdminPlannedFeatureSaveDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            if (string.IsNullOrWhiteSpace(request.TextFr) && string.IsNullOrWhiteSpace(request.TextEn))
                throw new Exception(_localizer["Admin_PlannedFeatureTextRequired"]);

            PlannedFeature feature;
            if (request.Id.HasValue)
            {
                feature = await context.PlannedFeatures.FirstOrDefaultAsync(x => x.Id == request.Id.Value)
                    ?? throw new Exception(_localizer["Admin_PlannedFeatureNotFound"]);
            }
            else
            {
                feature = new PlannedFeature
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.PlannedFeatures.Add(feature);
            }

            feature.TextFr = request.TextFr?.Trim() ?? string.Empty;
            feature.TextEn = request.TextEn?.Trim() ?? string.Empty;
            feature.DisplayOrder = request.DisplayOrder;
            feature.IsActive = request.IsActive;
            feature.UpdatedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return MapPlannedFeature(feature);
        }

        public async Task DeletePlannedFeatureAsync(Guid administratorUserAccountId, Guid plannedFeatureId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var feature = await context.PlannedFeatures.FirstOrDefaultAsync(x => x.Id == plannedFeatureId)
                ?? throw new Exception(_localizer["Admin_PlannedFeatureNotFound"]);

            context.PlannedFeatures.Remove(feature);
            await context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(Guid administratorUserAccountId, AdminUserUpdateDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var user = await context.UserAccounts.FirstOrDefaultAsync(x => x.Id == request.UserId)
                ?? throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            var username = (request.Username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception(_localizer["Backend_UsernameRequired"]);

            var normalizedEmail = NormalizeEmail(request.Email);
            if (!string.IsNullOrWhiteSpace(normalizedEmail) && !IsValidEmail(normalizedEmail))
                throw new Exception(_localizer["Backend_EmailInvalidFormat"]);

            var usernameExists = await context.UserAccounts
                .AnyAsync(x => x.Id != user.Id && x.Username.ToLower() == username.ToLower());

            if (usernameExists)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var emailExists = await context.UserAccounts
                    .AnyAsync(x => x.Id != user.Id && x.Email == normalizedEmail);

                if (emailExists)
                    throw new Exception(_localizer["Backend_EmailAlreadyExists"]);
            }

            var emailChanged = user.Email != normalizedEmail;

            user.Username = username;
            user.Email = normalizedEmail;
            user.IsEmailVerified = emailChanged ? false : request.IsEmailVerified;
            user.IsGameMaster = request.IsGameMaster;
            user.WantsToBeGameMaster = request.WantsToBeGameMaster;
            user.Language = NormalizeLanguage(request.Language);
            user.MaxPlayersPerSession = Math.Clamp(request.MaxPlayersPerSession, 0, 5000);

            if (request.CurrentSubscriptionPlanId.HasValue)
            {
                var plan = await context.SubscriptionPlans
                    .FirstOrDefaultAsync(x => x.Id == request.CurrentSubscriptionPlanId.Value)
                    ?? throw new Exception(_localizer["Admin_SubscriptionPlanNotFound"]);

                var subscription = await context.UserSubscriptions.FirstOrDefaultAsync(x => x.UserAccountId == user.Id);
                if (subscription is null)
                {
                    context.UserSubscriptions.Add(new UserSubscription
                    {
                        Id = Guid.NewGuid(),
                        UserAccountId = user.Id,
                        CurrentPlanId = plan.Id,
                        StartedAtUtc = DateTime.UtcNow,
                        NextRenewalAtUtc = DateTime.UtcNow.AddMonths(1),
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    subscription.CurrentPlanId = plan.Id;
                    subscription.PendingPlanId = null;
                    subscription.CancelAtRenewal = false;
                    subscription.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            if (emailChanged)
            {
                user.LastSensitiveChangeAtUtc = DateTime.UtcNow;
                user.LastSensitiveChangeType = "Account_SecurityHistory_EmailChanged";
            }

            await context.SaveChangesAsync();
        }

        public async Task SendUserEmailVerificationAsync(Guid administratorUserAccountId, Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            await _accountSecurityService.SendEmailVerificationAsync(userAccountId);
        }

        public async Task SendUserPasswordResetAsync(Guid administratorUserAccountId, Guid userAccountId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var user = await context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userAccountId)
                ?? throw new Exception(_localizer["Backend_UserAccountNotFound"]);

            await _accountSecurityService.RequestPasswordResetAsync(string.IsNullOrWhiteSpace(user.Email) ? user.Username : user.Email);
        }

        public async Task<AdminSubscriptionPlanDto> SaveSubscriptionPlanAsync(Guid administratorUserAccountId, AdminSubscriptionPlanSaveDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var code = (request.Code ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(code))
                throw new Exception(_localizer["Admin_SubscriptionPlanCodeRequired"]);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception(_localizer["Admin_SubscriptionPlanNameRequired"]);

            var duplicateCode = await context.SubscriptionPlans
                .AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value));

            if (duplicateCode)
                throw new Exception(_localizer["Admin_SubscriptionPlanCodeAlreadyExists"]);

            SubscriptionPlan plan;
            if (request.Id.HasValue)
            {
                plan = await context.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == request.Id.Value)
                    ?? throw new Exception(_localizer["Admin_SubscriptionPlanNotFound"]);
            }
            else
            {
                plan = new SubscriptionPlan { Id = Guid.NewGuid() };
                context.SubscriptionPlans.Add(plan);
            }

            plan.Code = code;
            plan.Name = name;
            plan.MonthlyPriceTtc = request.MonthlyPriceTtc < 0 ? 0 : request.MonthlyPriceTtc;
            plan.MaxPlayersPerSession = Math.Clamp(request.MaxPlayersPerSession, 0, 5000);
            plan.DisplayOrder = request.DisplayOrder;
            plan.IsActive = request.IsActive;

            await context.SaveChangesAsync();

            var subscriptions = await context.UserSubscriptions.AsNoTracking().ToListAsync();
            return BuildAdminSubscriptionPlans(await context.SubscriptionPlans.AsNoTracking().ToListAsync(), subscriptions)
                .First(x => x.Id == plan.Id);
        }

        public async Task DeleteSubscriptionPlanAsync(Guid administratorUserAccountId, Guid subscriptionPlanId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var plan = await context.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == subscriptionPlanId)
                ?? throw new Exception(_localizer["Admin_SubscriptionPlanNotFound"]);

            var isUsed = await context.UserSubscriptions.AnyAsync(x => x.CurrentPlanId == subscriptionPlanId || x.PendingPlanId == subscriptionPlanId);
            if (isUsed)
                throw new Exception(_localizer["Admin_SubscriptionPlanDeleteBlocked"]);

            context.SubscriptionPlans.Remove(plan);
            await context.SaveChangesAsync();
        }

        public async Task MakeGameSystemGenericAsync(Guid administratorUserAccountId, Guid gameSystemId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var gameSystem = await context.GameSystems.FirstOrDefaultAsync(x => x.Id == gameSystemId)
                ?? throw new Exception(_localizer["GameSystem_NotFound"]);

            gameSystem.IsGeneric = true;
            gameSystem.LockedToSessionId = null;
            await context.SaveChangesAsync();
        }

        public async Task DeleteGameSystemAsync(Guid administratorUserAccountId, Guid gameSystemId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var gameSystem = await context.GameSystems.FirstOrDefaultAsync(x => x.Id == gameSystemId)
                ?? throw new Exception(_localizer["GameSystem_NotFound"]);

            var isUsedBySession = await context.Sessions.AnyAsync(x => x.GameSystemId == gameSystemId);
            if (isUsedBySession)
                throw new Exception(_localizer["Admin_GameSystemDeleteBlocked"]);

            await DeleteGameSystemGraphAsync(context, gameSystem);
            await context.SaveChangesAsync();
        }

        public async Task SetSessionActiveAsync(Guid administratorUserAccountId, Guid sessionId, bool isActive)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var session = await context.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId)
                ?? throw new Exception(_localizer["Session_NotFound"]);

            session.IsActive = isActive;
            await context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(Guid administratorUserAccountId, Guid sessionId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await EnsureAdministratorAsync(context, administratorUserAccountId);

            var session = await context.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId)
                ?? throw new Exception(_localizer["Session_NotFound"]);

            await DeleteSessionGraphAsync(context, session);
            await context.SaveChangesAsync();
        }

        private async Task EnsureAdministratorAsync(RollocracyDbContext context, Guid userAccountId)
        {
            if (!await IsAdministratorCoreAsync(context, userAccountId))
                throw new Exception(_localizer["Admin_AccessDenied"]);
        }

        private static async Task<bool> IsAdministratorCoreAsync(RollocracyDbContext context, Guid userAccountId)
        {
            return await context.UserAccounts
                .AsNoTracking()
                .AnyAsync(x => x.Id == userAccountId && x.IsGameMaster);
        }

        private static async Task<List<AdminPatchNoteDto>> BuildAdminPatchNotesAsync(RollocracyDbContext context)
        {
            var notes = await context.PatchNotes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.PublishedAtUtc)
                .ToListAsync();

            var noteIds = notes.Select(x => x.Id).ToList();
            var entries = await context.PatchNoteEntries
                .AsNoTracking()
                .Where(x => noteIds.Contains(x.PatchNoteId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return notes.Select(note => new AdminPatchNoteDto
            {
                Id = note.Id,
                Version = note.Version,
                IsPublished = note.IsPublished,
                DisplayOrder = note.DisplayOrder,
                PublishedAtUtc = note.PublishedAtUtc,
                CreatedAtUtc = note.CreatedAtUtc,
                UpdatedAtUtc = note.UpdatedAtUtc,
                Entries = entries
                    .Where(entry => entry.PatchNoteId == note.Id)
                    .Select(entry => new AdminPatchNoteEntryDto
                    {
                        Id = entry.Id,
                        TextFr = entry.TextFr,
                        TextEn = entry.TextEn,
                        DisplayOrder = entry.DisplayOrder
                    })
                    .ToList()
            }).ToList();
        }

        private static async Task<List<AdminPlannedFeatureDto>> BuildAdminPlannedFeaturesAsync(RollocracyDbContext context)
        {
            var features = await context.PlannedFeatures
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CreatedAtUtc)
                .ToListAsync();

            return features.Select(MapPlannedFeature).ToList();
        }

        private static AdminPlannedFeatureDto MapPlannedFeature(PlannedFeature feature)
        {
            return new AdminPlannedFeatureDto
            {
                Id = feature.Id,
                TextFr = feature.TextFr,
                TextEn = feature.TextEn,
                DisplayOrder = feature.DisplayOrder,
                IsActive = feature.IsActive
            };
        }

        private static List<AdminUserDto> BuildAdminUsers(
            List<UserAccount> users,
            List<UserSubscription> subscriptions,
            List<SubscriptionPlan> plans)
        {
            return users.Select(user =>
            {
                var subscription = subscriptions.FirstOrDefault(x => x.UserAccountId == user.Id);
                var plan = subscription is null ? null : plans.FirstOrDefault(x => x.Id == subscription.CurrentPlanId);

                return new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    IsEmailVerified = user.IsEmailVerified,
                    IsGameMaster = user.IsGameMaster,
                    WantsToBeGameMaster = user.WantsToBeGameMaster,
                    IsTwitchLinked = user.IsTwitchLinked,
                    TwitchLogin = user.TwitchLogin,
                    Language = user.Language,
                    MaxPlayersPerSession = user.MaxPlayersPerSession,
                    CurrentSubscriptionPlanId = plan?.Id,
                    CurrentSubscriptionPlanCode = plan?.Code,
                    CurrentSubscriptionPlanName = plan?.Name,
                    CreatedAtUtc = user.CreatedAt
                };
            }).ToList();
        }

        private static List<AdminSubscriptionPlanDto> BuildAdminSubscriptionPlans(
            List<SubscriptionPlan> plans,
            List<UserSubscription> subscriptions)
        {
            return plans
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Code)
                .Select(plan => new AdminSubscriptionPlanDto
                {
                    Id = plan.Id,
                    Code = plan.Code,
                    Name = plan.Name,
                    MonthlyPriceTtc = plan.MonthlyPriceTtc,
                    MaxPlayersPerSession = plan.MaxPlayersPerSession,
                    DisplayOrder = plan.DisplayOrder,
                    IsActive = plan.IsActive,
                    UserCount = subscriptions.Count(x => x.CurrentPlanId == plan.Id || x.PendingPlanId == plan.Id)
                })
                .ToList();
        }

        private static List<AdminGameSystemDto> BuildAdminGameSystems(
            List<GameSystem> gameSystems,
            List<UserAccount> users,
            List<Session> sessions)
        {
            return gameSystems.Select(system => new AdminGameSystemDto
            {
                Id = system.Id,
                Name = system.Name,
                CreatorUsername = users.FirstOrDefault(x => x.Id == system.OwnerUserAccountId)?.Username ?? string.Empty,
                TestResolutionMode = system.TestResolutionMode,
                SessionsUsingCount = sessions.Count(x => x.GameSystemId == system.Id),
                IsGeneric = system.IsGeneric,
                IsLockedToSession = system.LockedToSessionId.HasValue
            }).ToList();
        }

        private static List<AdminSessionDto> BuildAdminSessions(
            List<Session> sessions,
            List<UserAccount> users,
            List<GameSystem> gameSystems,
            List<PlayerSession> playerSessions,
            List<Character> characters,
            string normalizedBaseUri)
        {
            return sessions.Select(session =>
            {
                var sessionPlayerIds = playerSessions
                    .Where(x => x.SessionId == session.Id)
                    .Select(x => x.Id)
                    .ToHashSet();

                var sessionCharacters = characters.Where(x => sessionPlayerIds.Contains(x.PlayerSessionId)).ToList();
                var gm = users.FirstOrDefault(x => x.Id == session.GameMasterUserAccountId);

                return new AdminSessionDto
                {
                    Id = session.Id,
                    GameMasterUsername = gm?.Username ?? string.Empty,
                    SessionName = session.SessionName,
                    SessionSlug = session.SessionSlug,
                    IsActive = session.IsActive,
                    GameSystemId = session.GameSystemId,
                    GameSystemName = session.GameSystemId.HasValue
                        ? gameSystems.FirstOrDefault(x => x.Id == session.GameSystemId.Value)?.Name
                        : null,
                    AliveCharacterCount = sessionCharacters.Count(x => x.IsAlive),
                    TotalCharacterCount = sessionCharacters.Count,
                    JoinUrl = gm is null || string.IsNullOrWhiteSpace(session.SessionSlug)
                        ? string.Empty
                        : $"{normalizedBaseUri}{gm.Username}/{session.SessionSlug}",
                    CreatedAtUtc = session.CreatedAt
                };
            }).ToList();
        }

        private static async Task DeleteGameSystemGraphAsync(RollocracyDbContext context, GameSystem gameSystem)
        {
            var gameSystemId = gameSystem.Id;

            var metricDefinitions = await context.MetricDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync();
            var metricIds = metricDefinitions.Select(x => x.Id).ToList();
            context.MetricFormulaSteps.RemoveRange(await context.MetricFormulaSteps.Where(x => metricIds.Contains(x.MetricDefinitionId)).ToListAsync());
            context.MetricComponents.RemoveRange(await context.MetricComponents.Where(x => metricIds.Contains(x.MetricDefinitionId)).ToListAsync());
            context.MetricDefinitions.RemoveRange(metricDefinitions);

            var derivedDefinitions = await context.DerivedStatDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync();
            var derivedIds = derivedDefinitions.Select(x => x.Id).ToList();
            context.DerivedStatComponents.RemoveRange(await context.DerivedStatComponents.Where(x => derivedIds.Contains(x.DerivedStatDefinitionId)).ToListAsync());
            context.DerivedStatDefinitions.RemoveRange(derivedDefinitions);

            var traitDefinitions = await context.TraitDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync();
            var traitIds = traitDefinitions.Select(x => x.Id).ToList();
            var traitOptions = await context.TraitOptions.Where(x => traitIds.Contains(x.TraitDefinitionId)).ToListAsync();
            var traitOptionIds = traitOptions.Select(x => x.Id).ToList();
            context.ChoiceOptionModifierDefinitions.RemoveRange(await context.ChoiceOptionModifierDefinitions.Where(x => traitOptionIds.Contains(x.ChoiceOptionDefinitionId)).ToListAsync());
            context.TraitOptions.RemoveRange(traitOptions);
            context.TraitDefinitions.RemoveRange(traitDefinitions);

            var talentDefinitions = await context.TalentDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync();
            var talentIds = talentDefinitions.Select(x => x.Id).ToList();
            context.TalentModifierDefinitions.RemoveRange(await context.TalentModifierDefinitions.Where(x => talentIds.Contains(x.TalentDefinitionId)).ToListAsync());
            context.TalentDefinitions.RemoveRange(talentDefinitions);

            var itemDefinitions = await context.ItemDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync();
            var itemIds = itemDefinitions.Select(x => x.Id).ToList();
            context.ItemModifierDefinitions.RemoveRange(await context.ItemModifierDefinitions.Where(x => itemIds.Contains(x.ItemDefinitionId)).ToListAsync());
            context.ItemDefinitions.RemoveRange(itemDefinitions);

            context.GaugeDefinitions.RemoveRange(await context.GaugeDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync());
            context.AttributeDefinitions.RemoveRange(await context.AttributeDefinitions.Where(x => x.GameSystemId == gameSystemId).ToListAsync());
            context.GameSystemSnapshots.RemoveRange(await context.GameSystemSnapshots.Where(x => x.GameSystemId == gameSystemId).ToListAsync());
            context.GameSystems.Remove(gameSystem);
        }

        private static async Task DeleteSessionGraphAsync(RollocracyDbContext context, Session session)
        {
            var sessionId = session.Id;

            var playerSessions = await context.PlayerSessions.Where(x => x.SessionId == sessionId).ToListAsync();
            var playerSessionIds = playerSessions.Select(x => x.Id).ToList();
            var characters = await context.Characters.Where(x => playerSessionIds.Contains(x.PlayerSessionId)).ToListAsync();
            var characterIds = characters.Select(x => x.Id).ToList();

            context.CharacterModifiers.RemoveRange(await context.CharacterModifiers.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.CharacterAttributeValues.RemoveRange(await context.CharacterAttributeValues.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.CharacterGaugeValues.RemoveRange(await context.CharacterGaugeValues.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.CharacterTraitValues.RemoveRange(await context.CharacterTraitValues.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.CharacterTalents.RemoveRange(await context.CharacterTalents.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.CharacterItems.RemoveRange(await context.CharacterItems.Where(x => characterIds.Contains(x.CharacterId)).ToListAsync());
            context.Characters.RemoveRange(characters);

            var testIds = await context.GameTests.Where(x => x.SessionId == sessionId).Select(x => x.Id).ToListAsync();
            context.PlayerTestRolls.RemoveRange(await context.PlayerTestRolls.Where(x => testIds.Contains(x.GameTestId)).ToListAsync());
            context.GameTestConsequences.RemoveRange(await context.GameTestConsequences.Where(x => testIds.Contains(x.GameTestId)).ToListAsync());
            context.GameTestTraitFilters.RemoveRange(await context.GameTestTraitFilters.Where(x => testIds.Contains(x.GameTestId)).ToListAsync());
            context.GameTestAppliedEffects.RemoveRange(await context.GameTestAppliedEffects.Where(x => testIds.Contains(x.GameTestId)).ToListAsync());
            context.GameTests.RemoveRange(await context.GameTests.Where(x => x.SessionId == sessionId).ToListAsync());
            context.SessionGameTestPresets.RemoveRange(await context.SessionGameTestPresets.Where(x => x.SessionId == sessionId).ToListAsync());

            var pollIds = await context.SessionPolls.Where(x => x.SessionId == sessionId).Select(x => x.Id).ToListAsync();
            var pollOptionIds = await context.SessionPollOptions.Where(x => pollIds.Contains(x.SessionPollId)).Select(x => x.Id).ToListAsync();
            context.SessionPollOptionConsequences.RemoveRange(await context.SessionPollOptionConsequences.Where(x => pollOptionIds.Contains(x.SessionPollOptionId)).ToListAsync());
            context.SessionPollVotes.RemoveRange(await context.SessionPollVotes.Where(x => pollIds.Contains(x.SessionPollId)).ToListAsync());
            context.SessionPollEligibleCharacters.RemoveRange(await context.SessionPollEligibleCharacters.Where(x => pollIds.Contains(x.SessionPollId)).ToListAsync());
            context.SessionPollWeightRules.RemoveRange(await context.SessionPollWeightRules.Where(x => pollIds.Contains(x.SessionPollId)).ToListAsync());
            context.SessionPollAppliedEffects.RemoveRange(await context.SessionPollAppliedEffects.Where(x => pollIds.Contains(x.SessionPollId)).ToListAsync());
            context.SessionPollOptions.RemoveRange(await context.SessionPollOptions.Where(x => pollIds.Contains(x.SessionPollId)).ToListAsync());
            context.SessionPolls.RemoveRange(await context.SessionPolls.Where(x => x.SessionId == sessionId).ToListAsync());
            context.SessionPollPresets.RemoveRange(await context.SessionPollPresets.Where(x => x.SessionId == sessionId).ToListAsync());

            var store = await context.SessionStores.FirstOrDefaultAsync(x => x.SessionId == sessionId);
            if (store is not null)
            {
                context.SessionStoreOffers.RemoveRange(await context.SessionStoreOffers.Where(x => x.SessionStoreId == store.Id).ToListAsync());
                context.SessionStores.Remove(store);
            }

            context.SessionJournalPages.RemoveRange(await context.SessionJournalPages.Where(x => x.SessionId == sessionId).ToListAsync());
            context.SessionGauges.RemoveRange(await context.SessionGauges.Where(x => x.SessionId == sessionId).ToListAsync());
            context.SessionRandomDraws.RemoveRange(await context.SessionRandomDraws.Where(x => x.SessionId == sessionId).ToListAsync());
            context.MassDistributionBatches.RemoveRange(await context.MassDistributionBatches.Where(x => x.SessionId == sessionId).ToListAsync());

            var sessionItems = await context.ItemDefinitions.Where(x => x.SessionId == sessionId).ToListAsync();
            var sessionItemIds = sessionItems.Select(x => x.Id).ToList();
            context.ItemModifierDefinitions.RemoveRange(await context.ItemModifierDefinitions.Where(x => sessionItemIds.Contains(x.ItemDefinitionId)).ToListAsync());
            context.ItemDefinitions.RemoveRange(sessionItems);

            context.PlayerSessions.RemoveRange(playerSessions);
            context.Sessions.Remove(session);
        }

        private static bool IsMeaningfulPatchNoteEntry(AdminPatchNoteEntryDto entry)
        {
            return !string.IsNullOrWhiteSpace(entry.TextFr) || !string.IsNullOrWhiteSpace(entry.TextEn);
        }

        private static string GetLocalizedText(string textFr, string textEn, string language)
        {
            if (language == "en")
                return string.IsNullOrWhiteSpace(textEn) ? textFr : textEn;

            return string.IsNullOrWhiteSpace(textFr) ? textEn : textFr;
        }

        private static string NormalizeLanguage(string? language)
        {
            return string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr";
        }

        private static string? NormalizeEmail(string? email)
        {
            var normalized = email?.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var parsed = new System.Net.Mail.MailAddress(email.Trim());
                return string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}