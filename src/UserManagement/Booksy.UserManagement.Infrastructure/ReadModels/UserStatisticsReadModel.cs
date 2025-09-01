// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
// ========================================
// Booksy.UserManagement.Infrastructure/ReadModels/UserStatisticsReadModel.cs
// ========================================
namespace Booksy.UserManagement.Infrastructure.ReadModels
{
    /// <summary>
    /// Read model for user statistics and analytics
    /// </summary>
    public class UserStatisticsReadModel
    {
        public DateTime Date { get; set; }
        public int TotalUsers { get; set; }
        public int NewRegistrations { get; set; }
        public int ActiveUsers { get; set; }
        public int UniqueLogins { get; set; }
        public Dictionary<string, int> UsersByType { get; set; } = new();
        public Dictionary<string, int> UsersByStatus { get; set; } = new();
        public Dictionary<string, int> RegistrationsByHour { get; set; } = new();
        public double AverageSessionDuration { get; set; }
        public double DailyActiveRate { get; set; }
        public double WeeklyActiveRate { get; set; }
        public double MonthlyActiveRate { get; set; }
        public int PasswordResets { get; set; }
        public int FailedLogins { get; set; }
        public int AccountLocks { get; set; }
    }
}


