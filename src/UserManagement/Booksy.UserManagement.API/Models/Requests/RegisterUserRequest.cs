using System.ComponentModel.DataAnnotations;

public class RegisterUserRequest
{
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [RegularExpression("^(Client|Provider|Admin)$")]
    public string UserType { get; set; } = "Client";

    [Required]
    public bool AcceptTerms { get; set; }

    public bool MarketingConsent { get; set; }

    [MaxLength(50)]
    public string? ReferralCode { get; set; }
}