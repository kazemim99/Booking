

// ========================================
// Models/Requests/Other Requests
// ========================================
using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.UserManagement.API.Models.Requests;

public class DisableTwoFactorRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
