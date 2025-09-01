// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
namespace Booksy.UserManagement.Application.CQRS.Commands.RefreshToken
{
    public class RefreshTokenResult
    {
        public RefreshTokenResult(string AccessToken, string RefreshToken, int ExpiresIn)
        {
            this.AccessToken = AccessToken;
            this.RefreshToken = RefreshToken;
            this.ExpiresIn = ExpiresIn;
        }

        public string AccessToken { get; }
        public string RefreshToken { get; }
        public int ExpiresIn { get; }
    }
}