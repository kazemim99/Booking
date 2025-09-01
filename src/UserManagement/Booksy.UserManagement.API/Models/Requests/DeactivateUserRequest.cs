// ========================================
// Models/Requests/DeactivateUserRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request model for deactivating a user account
/// </summary>
public class DeactivateUserRequest
{
    /// <summary>
    /// Reason for deactivation
    /// </summary>
    [Required(ErrorMessage = "Deactivation reason is required")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes (optional)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to send notification to user
    /// </summary>
    public bool SendNotification { get; set; } = true;
}


