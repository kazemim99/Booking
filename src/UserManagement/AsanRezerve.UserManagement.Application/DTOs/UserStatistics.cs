// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// AsanRezerve.UserManagement.Application/DTOs/UserProfileDto.cs
namespace AsanRezerve.UserManagement.Application.DTOs
{
    public sealed class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int DeletedUsers { get; set; }
        public Dictionary<string, int> UsersByType { get; set; } = new();
        public int UsersRegisteredToday { get; set; }
        public int UsersRegisteredThisWeek { get; set; }
        public int UsersRegisteredThisMonth { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; }
    }
}
