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
