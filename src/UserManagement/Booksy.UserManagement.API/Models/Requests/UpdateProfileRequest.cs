
// ========================================
// Models/Requests/Other Requests
// ========================================
namespace Booksy.UserManagement.API.Models.Requests;

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
