
// ========================================
// Models/Responses/AuthenticationResponse.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

public class UserInfoResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}
