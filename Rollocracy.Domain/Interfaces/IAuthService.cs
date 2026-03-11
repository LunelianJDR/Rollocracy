using Rollocracy.Domain.Entities;

namespace Rollocracy.Domain.Interfaces
{
    public interface IAuthService
    {
        // Création d'un compte utilisateur
        Task<UserAccount> RegisterAsync(string username, string password, bool isGameMaster);

        // Vérification des identifiants lors du login
        Task<UserAccount?> ValidateLoginAsync(string username, string password);

        // Récupérer un utilisateur par son nom
        Task<UserAccount?> GetUserByUsernameAsync(string username);
    }
}