using Microsoft.EntityFrameworkCore;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.GameRules;
using Rollocracy.Domain.GameTests;
using Rollocracy.Infrastructure.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollocracy.Infrastructure.Persistence
{
    /// DbContext principal de l'application.
    /// Représente la base de données PostgreSQL.
    public class RollocracyDbContext : DbContext
    {
        public RollocracyDbContext(DbContextOptions<RollocracyDbContext> options)
            : base(options)
        {
        }

        /// Streamers enregistrés
        public DbSet<Streamer> Streamers => Set<Streamer>();

        /// Systèmes de jeu créés par les MJ
        public DbSet<GameSystem> GameSystems => Set<GameSystem>();

        /// Sessions de jeu
        public DbSet<Session> Sessions => Set<Session>();

        /// Joueurs présents dans une session
        public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();

        /// Personnages des joueurs
        public DbSet<Character> Characters => Set<Character>();

        /// Définition des attributs
        public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();

        /// Valeurs d'attributs pour les personnages
        public DbSet<CharacterAttributeValue> CharacterAttributeValues => Set<CharacterAttributeValue>();

        /// Tests demandés par le MJ
        public DbSet<GameTest> GameTests => Set<GameTest>();

        /// Résultats des jets
        public DbSet<PlayerTestRoll> PlayerTestRolls => Set<PlayerTestRoll>();

        /// Conséquences des tests
        public DbSet<GameTestConsequence> GameTestConsequences => Set<GameTestConsequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ici on ajoutera plus tard les configurations avancées
        }

        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    }
}
