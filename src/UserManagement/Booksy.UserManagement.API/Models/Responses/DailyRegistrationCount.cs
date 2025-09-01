// ========================================
// Models/Responses/DailyRegistrationCount.cs
// ========================================
namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Daily registration count for statistics
/// </summary>
public class DailyRegistrationCount
{
    /// <summary>
    /// Date of registration
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of registrations on that date
    /// </summary>
    public int Count { get; set; }
}