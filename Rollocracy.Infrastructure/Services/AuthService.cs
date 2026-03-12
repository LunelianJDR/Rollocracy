using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Rollocracy.Domain.Entities;
using Rollocracy.Domain.Interfaces;
using Rollocracy.Infrastructure.Persistence;

namespace Rollocracy.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<RollocracyDbContext> _contextFactory;
        private readonly IStringLocalizer _localizer;

        // Hasher officiel Microsoft pour les mots de passe de compte
        private readonly PasswordHasher<UserAccount> _passwordHasher = new();

        public AuthService(
            IDbContextFactory<RollocracyDbContext> contextFactory,
            IStringLocalizerFactory localizerFactory)
        {
            _contextFactory = contextFactory;

            // Utilise le fichier de ressources partagé de l'application web
            _localizer = localizerFactory.Create("Rollocracy.Localization.SharedTexts", "Rollocracy");
        }

        // Création d'un compte utilisateur
        public async Task<UserAccount> RegisterAsync(string username, string password, bool isGameMaster, string language)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Vérifie si le pseudo existe déjà
            var existingUser = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (existingUser != null)
                throw new Exception(_localizer["Backend_UsernameAlreadyExists"]);

            // Sécurise la langue reçue
            var normalizedLanguage = NormalizeLanguage(language);

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Username = username,
                IsGameMaster = isGameMaster,
                Language = normalizedLanguage
            };

            // Génération du hash sécurisé
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            context.UserAccounts.Add(user);

            await context.SaveChangesAsync();

            return user;
        }

        // Vérifie login + mot de passe
        public async Task<UserAccount?> ValidateLoginAsync(string username, string password)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Success)
                return user;

            return null;
        }

        // Récupération simple d'utilisateur
        public async Task<UserAccount?> GetUserByUsernameAsync(string username)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.UserAccounts
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        // Normalise la langue pour éviter les valeurs incohérentes
        private string NormalizeLanguage(string language)
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