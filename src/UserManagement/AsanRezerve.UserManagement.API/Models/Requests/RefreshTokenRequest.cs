

using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.UserManagement.API.Models.Requests;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
