// ========================================
// Booksy.ServiceCatalog.API/Models/Requests/AddNotesRequest.cs
// ========================================
using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests
{
    /// <summary>
    /// Request model for adding notes to a booking
    /// </summary>
    public class AddNotesRequest
    {
        /// <summary>
        /// Notes content
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Whether these are staff notes (internal) or customer notes
        /// </summary>
        public bool IsStaffNote { get; set; } = false;
    }
}
