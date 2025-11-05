using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for marking a booking as no-show
/// </summary>
public class MarkNoShowRequest
{
    /// <summary>
    /// Notes about the no-show incident
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
