
namespace Booksy.ServiceCatalog.API.Models.Requests
{

    public sealed class UpdateVerificationRequest
    {
        [Required(ErrorMessage = "Verification status is required")]
        public bool IsVerified { get; set; }

        [StringLength(1000, ErrorMessage = "Verification notes cannot exceed 1000 characters")]
        public string? VerificationNotes { get; set; }
    }
}

