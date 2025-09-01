// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// Booksy.UserManagement.Application/DTOs/UserStatisticsDto.cs
namespace Booksy.UserManagement.Application.DTOs
{
    public sealed class UserStatisticsDto
    {
        public int TotalUsers { get; init; }
        public int ActiveUsers { get; init; }
        public int PendingUsers { get; init; }
        public int SuspendedUsers { get; init; }
        public Dictionary<string, int> UsersByType { get; init; } = new();
        public Dictionary<string, int> UsersByRole { get; init; } = new();
        public int RegisteredToday { get; init; }
        public int RegisteredThisWeek { get; init; }
        public int RegisteredThisMonth { get; init; }
        public double DailyGrowthRate { get; init; }
        public double WeeklyGrowthRate { get; init; }
        public double MonthlyGrowthRate { get; init; }
    }
}

