

// ========================================
// Models/Requests/Other Requests
// ========================================
using Booksy.Core.Application.DTOs;
using Booksy.UserManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

public class ActivateUserRequest
{
    [Required]
    [MaxLength(200)]
    public string ActivationToken { get; set; } = string.Empty;
}


public class GetUsersRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [RegularExpression("^(Active|Inactive|PendingActivation|Suspended|Deleted)$")]
    public UserStatus? Status { get; set; }

    [RegularExpression("^(Client|Provider|Admin)$")]
    public UserType? UserType { get; set; }

    [RegularExpression("^(RegisteredAt|LastLoginAt|Email|FirstName|LastName)$")]
    public string SortBy { get; set; } = "RegisteredAt";

    public SortDirection SortDirection { get; set; }
}



public class UpdateUserProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Url]
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
}




