using Microsoft.EntityFrameworkCore;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Infrastructure.Events;
using Rollocracy.Domain.Polls;

namespace Rollocracy.Infrastructure.Persistence
{
    public class RollocracyDbContext : DbContext
    {
        public RollocracyDbContext(DbContextOptions<RollocracyDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<GameSystem> GameSystems => Set<GameSystem>();

        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();
        public DbSet<Character> Characters => Set<Character>();

        // Caractéristiques numériques
        public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
        public DbSet<CharacterAttributeValue> CharacterAttributeValues => Set<CharacterAttributeValue>();

        // Attributs à choix
        public DbSet<TraitDefinition> TraitDefinitions => Set<TraitDefinition>();
        public DbSet<TraitOption> TraitOptions => Set<TraitOption>();
        public DbSet<CharacterTraitValue> CharacterTraitValues => Set<CharacterTraitValue>();

        // Jauges
        public DbSet<GaugeDefinition> GaugeDefinitions => Set<GaugeDefinition>();
        public DbSet<CharacterGaugeValue> CharacterGaugeValues => Set<CharacterGaugeValue>();

        public DbSet<GameTest> GameTests => Set<GameTest>();
        public DbSet<PlayerTestRoll> PlayerTestRolls => Set<PlayerTestRoll>();
        public DbSet<GameTestConsequence> GameTestConsequences => Set<GameTestConsequence>();
        public DbSet<GameTestTraitFilter> GameTestTraitFilters => Set<GameTestTraitFilter>();
        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
        public DbSet<GameTestAppliedEffect> GameTestAppliedEffects => Set<GameTestAppliedEffect>();

        // Sondages
        public DbSet<SessionPoll> SessionPolls => Set<SessionPoll>();
        public DbSet<SessionPollOption> SessionPollOptions => Set<SessionPollOption>();
        public DbSet<SessionPollVote> SessionPollVotes => Set<SessionPollVote>();
        public DbSet<SessionPollWeightRule> SessionPollWeightRules => Set<SessionPollWeightRule>();
        public DbSet<SessionPollOptionConsequence> SessionPollOptionConsequences => Set<SessionPollOptionConsequence>();
        public DbSet<SessionPollAppliedEffect> SessionPollAppliedEffects => Set<SessionPollAppliedEffect>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasIndex(u => u.Username)
                    .IsUnique();

                entity.Property(u => u.MaxPlayersPerSession)
                    .HasDefaultValue(0);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_UserAccounts_MaxPlayersPerSession_Range",
                        "\"MaxPlayersPerSession\" >= 0 AND \"MaxPlayersPerSession\" <= 5000");
                });
            });

            modelBuilder.Entity<Session>()
                .HasIndex(s => new { s.GameMasterUserAccountId, s.SessionSlug })
                .IsUnique();
        }
    }
}