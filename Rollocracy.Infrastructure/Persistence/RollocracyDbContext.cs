using Microsoft.EntityFrameworkCore;
using Rollocracy.Domain.Characters;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Domain.Polls;
using Rollocracy.Infrastructure.Events;

namespace Rollocracy.Infrastructure.Persistence
{
    public class RollocracyDbContext : DbContext
    {
        public RollocracyDbContext(DbContextOptions<RollocracyDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<AccountSecurityToken> AccountSecurityTokens => Set<AccountSecurityToken>();
        public DbSet<TwitchPendingAuthSession> TwitchPendingAuthSessions => Set<TwitchPendingAuthSession>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
        public DbSet<GameSystem> GameSystems => Set<GameSystem>();
        public DbSet<GameSystemSnapshot> GameSystemSnapshots => Set<GameSystemSnapshot>();

        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();
        public DbSet<Character> Characters => Set<Character>();
        public DbSet<CharacterModifier> CharacterModifiers => Set<CharacterModifier>();

        public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
        public DbSet<DerivedStatDefinition> DerivedStatDefinitions => Set<DerivedStatDefinition>();
        public DbSet<DerivedStatComponent> DerivedStatComponents => Set<DerivedStatComponent>();
        public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
        public DbSet<MetricComponent> MetricComponents => Set<MetricComponent>();
        public DbSet<MetricFormulaStep> MetricFormulaSteps => Set<MetricFormulaStep>();
        public DbSet<CharacterAttributeValue> CharacterAttributeValues => Set<CharacterAttributeValue>();

        public DbSet<TraitDefinition> TraitDefinitions => Set<TraitDefinition>();
        public DbSet<TraitOption> TraitOptions => Set<TraitOption>();
        public DbSet<CharacterTraitValue> CharacterTraitValues => Set<CharacterTraitValue>();

        public DbSet<GaugeDefinition> GaugeDefinitions => Set<GaugeDefinition>();
        public DbSet<CharacterGaugeValue> CharacterGaugeValues => Set<CharacterGaugeValue>();

        public DbSet<GameTest> GameTests => Set<GameTest>();
        public DbSet<PlayerTestRoll> PlayerTestRolls => Set<PlayerTestRoll>();
        public DbSet<GameTestConsequence> GameTestConsequences => Set<GameTestConsequence>();
        public DbSet<GameTestTraitFilter> GameTestTraitFilters => Set<GameTestTraitFilter>();
        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
        public DbSet<GameTestAppliedEffect> GameTestAppliedEffects => Set<GameTestAppliedEffect>();

        public DbSet<SessionPoll> SessionPolls => Set<SessionPoll>();
        public DbSet<SessionPollOption> SessionPollOptions => Set<SessionPollOption>();
        public DbSet<SessionPollVote> SessionPollVotes => Set<SessionPollVote>();
        public DbSet<SessionPollEligibleCharacter> SessionPollEligibleCharacters => Set<SessionPollEligibleCharacter>();
        public DbSet<SessionPollWeightRule> SessionPollWeightRules => Set<SessionPollWeightRule>();
        public DbSet<SessionPollOptionConsequence> SessionPollOptionConsequences => Set<SessionPollOptionConsequence>();
        public DbSet<SessionPollAppliedEffect> SessionPollAppliedEffects => Set<SessionPollAppliedEffect>();

        public DbSet<TalentDefinition> TalentDefinitions => Set<TalentDefinition>();
        public DbSet<TalentModifierDefinition> TalentModifierDefinitions => Set<TalentModifierDefinition>();

        public DbSet<ItemDefinition> ItemDefinitions => Set<ItemDefinition>();
        public DbSet<ItemModifierDefinition> ItemModifierDefinitions => Set<ItemModifierDefinition>();

        public DbSet<CharacterTalent> CharacterTalents => Set<CharacterTalent>();
        public DbSet<CharacterItem> CharacterItems => Set<CharacterItem>();

        public DbSet<ChoiceOptionModifierDefinition> ChoiceOptionModifierDefinitions => Set<ChoiceOptionModifierDefinition>();

        public DbSet<MassDistributionBatch> MassDistributionBatches => Set<MassDistributionBatch>();

        public DbSet<SessionGauge> SessionGauges => Set<SessionGauge>();

        public DbSet<SessionRandomDraw> SessionRandomDraws => Set<SessionRandomDraw>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasIndex(u => u.Username)
                    .IsUnique();

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.MaxPlayersPerSession)
                    .HasDefaultValue(0);

                entity.Property(u => u.Language)
                    .HasDefaultValue("fr");

                entity.Property(u => u.IsEmailVerified)
                    .HasDefaultValue(false);

                entity.Property(u => u.WantsToBeGameMaster)
                    .HasDefaultValue(false);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_UserAccounts_MaxPlayersPerSession_Range",
                        "\"MaxPlayersPerSession\" >= 0 AND \"MaxPlayersPerSession\" <= 5000");
                });
            });

            modelBuilder.Entity<AccountSecurityToken>(entity =>
            {
                entity.HasIndex(x => x.UserAccountId);
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => new { x.UserAccountId, x.Purpose });

                entity.Property(x => x.Purpose)
                    .HasColumnType("text");

                entity.Property(x => x.TokenHash)
                    .HasColumnType("text");

                entity.Property(x => x.EmailSnapshot)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<TwitchPendingAuthSession>(entity =>
            {
                entity.HasIndex(x => x.PublicToken).IsUnique();
                entity.HasIndex(x => x.OAuthState).IsUnique();
                entity.HasIndex(x => x.ExpiresAtUtc);
                entity.HasIndex(x => x.CurrentUserAccountId);
                entity.HasIndex(x => x.MatchedUserAccountId);

                entity.Property(x => x.PublicToken)
                    .HasColumnType("text");

                entity.Property(x => x.OAuthState)
                    .HasColumnType("text");

                entity.Property(x => x.FlowType)
                    .HasColumnType("text");

                entity.Property(x => x.TwitchUserId)
                    .HasColumnType("text");

                entity.Property(x => x.TwitchLogin)
                    .HasColumnType("text");

                entity.Property(x => x.TwitchDisplayName)
                    .HasColumnType("text");

                entity.Property(x => x.TwitchEmail)
                    .HasColumnType("text");

                entity.Property(x => x.Language)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();

                entity.Property(x => x.Code)
                    .HasColumnType("text");

                entity.Property(x => x.Name)
                    .HasColumnType("text");

                entity.Property(x => x.MonthlyPriceTtc)
                    .HasPrecision(18, 2);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_SubscriptionPlans_MaxPlayersPerSession_Range",
                        "\"MaxPlayersPerSession\" >= 0 AND \"MaxPlayersPerSession\" <= 5000");
                });

                entity.HasData(
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("0c1c29b4-c9b9-4e3b-a4aa-4106d7dd6a10"),
                        Code = "aventurier",
                        Name = "Aventurier",
                        MonthlyPriceTtc = 0m,
                        MaxPlayersPerSession = 15,
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("2f3df112-4d0d-4ef7-8b53-0c6fcb88b701"),
                        Code = "heros",
                        Name = "Héros",
                        MonthlyPriceTtc = 5m,
                        MaxPlayersPerSession = 75,
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("8c48c20d-5f5d-4b5a-b997-d863cf7be702"),
                        Code = "champion",
                        Name = "Champion",
                        MonthlyPriceTtc = 10m,
                        MaxPlayersPerSession = 200,
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("5fa71ab1-3f0c-401d-8c90-6b76a2d2c703"),
                        Code = "legende",
                        Name = "Légende",
                        MonthlyPriceTtc = 20m,
                        MaxPlayersPerSession = 500,
                        DisplayOrder = 4,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("c7c1d657-7fd4-42bd-b4f4-7047a6d43704"),
                        Code = "mythique",
                        Name = "Mythique",
                        MonthlyPriceTtc = 50m,
                        MaxPlayersPerSession = 1500,
                        DisplayOrder = 5,
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Id = Guid.Parse("8d9d4e4a-9d2b-4204-8c16-2f7ab0cb2f05"),
                        Code = "divin",
                        Name = "Divin",
                        MonthlyPriceTtc = 100m,
                        MaxPlayersPerSession = 5000,
                        DisplayOrder = 6,
                        IsActive = true
                    });
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasIndex(x => x.UserAccountId).IsUnique();
                entity.HasIndex(x => x.CurrentPlanId);
                entity.HasIndex(x => x.PendingPlanId);

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(x => x.StartedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(x => x.NextRenewalAtUtc)
                    .HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<Session>()
                .HasIndex(s => new { s.GameMasterUserAccountId, s.SessionSlug })
                .IsUnique();

            modelBuilder.Entity<GameSystemSnapshot>(entity =>
            {
                entity.HasIndex(x => new { x.GameSystemId, x.CreatedAtUtc });

                entity.Property(x => x.SnapshotJson)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<DerivedStatDefinition>()
                .HasIndex(x => new { x.GameSystemId, x.Name });

            modelBuilder.Entity<MetricDefinition>()
                .HasIndex(x => new { x.GameSystemId, x.Name });

            modelBuilder.Entity<DerivedStatComponent>()
                .HasIndex(x => new { x.DerivedStatDefinitionId, x.AttributeDefinitionId });

            modelBuilder.Entity<MetricComponent>()
                .HasIndex(x => new { x.MetricDefinitionId, x.AttributeDefinitionId });

            modelBuilder.Entity<MetricFormulaStep>(entity =>
            {
                entity.HasIndex(x => new { x.MetricDefinitionId, x.Order });

                entity.Property(x => x.ConstantValue)
                    .HasPrecision(18, 4);
            });

            modelBuilder.Entity<CharacterModifier>(entity =>
            {
                entity.HasIndex(x => x.CharacterId);
                entity.HasIndex(x => new { x.CharacterId, x.TargetType, x.TargetId });
                entity.HasIndex(x => new { x.SourceType, x.SourceId });

                entity.Property(x => x.SourceNameSnapshot)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<MassDistributionBatch>(entity =>
            {
                entity.HasIndex(x => x.SessionId);
                entity.HasIndex(x => new { x.SessionId, x.CreatedAtUtc });

                entity.Property(x => x.Name)
                    .HasColumnType("text");

                entity.Property(x => x.FilterSnapshotJson)
                    .HasColumnType("text");

                entity.Property(x => x.EffectsSnapshotJson)
                    .HasColumnType("text");

                entity.Property(x => x.UndoSnapshotJson)
                    .HasColumnType("text");

                entity.Property(x => x.UndoneAtUtc)
                    .HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<SessionGauge>(entity =>
            {
                entity.HasIndex(x => x.SessionId);
                entity.HasIndex(x => new { x.SessionId, x.Name });

                entity.Property(x => x.Name)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<SessionRandomDraw>(entity =>
            {
                entity.HasIndex(x => x.SessionId);
                entity.HasIndex(x => new { x.SessionId, x.CreatedAtUtc });

                entity.Property(x => x.Name)
                    .HasColumnType("text");

                entity.Property(x => x.ResultSnapshotJson)
                    .HasColumnType("text");
            });

            modelBuilder.Entity<SessionPollEligibleCharacter>(entity =>
            {
                entity.HasIndex(x => x.SessionPollId);
                entity.HasIndex(x => x.CharacterId);
                entity.HasIndex(x => new { x.SessionPollId, x.CharacterId }).IsUnique();
            });
        }
    }
}
