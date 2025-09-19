// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Testing.Builders;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;

namespace Booksy.UserManagement.Infrastructure.Persistence.Seeders
{
    public class UserManagementDatabaseSeeder : ISeeder
    {
        private readonly UserManagementDbContext _context;
        private readonly ILogger<UserManagementDatabaseSeeder> _logger;

        public UserManagementDatabaseSeeder(
            UserManagementDbContext context,
            ILogger<UserManagementDatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");

                // Apply pending migrations
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                    _logger.LogInformation("Applying pending migrations...");
                    await _context.Database.MigrateAsync();
                }

                // Seed data
                await SeedUsersAsync();
                await SeedRolesAsync();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task SeedUsersAsync()
        {
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Users already exist, skipping user seeding");
                return;
            }

            var users = new List<User>();

            // Create admin user using builder
            users.Add(UserBuilder.BuildAdmin());

            // Create test customer users using builder
            for (int i = 1; i <= 5; i++)
            {
                var isActive = i <= 3; // First 3 customers are active
                var customer = UserBuilder.BuildCustomer(
                    email: $"customer{i}@example.com",
                    firstName: $"Customer{i}",
                    lastName: $"User{i}",
                    isActive: isActive);

                users.Add(customer);
            }

            // Create test provider users using builder
            for (int i = 1; i <= 3; i++)
            {
                var address = Address.Create(
                    $"{i}00 Business Ave",
                    "Service City",
                    "SC",
                    $"5432{i}",
                    "USA");

                var provider = UserBuilder.BuildProvider(
                    email: $"provider{i}@business.com",
                    businessName: $"Provider Business {i}",
                    address: address);

                users.Add(provider);
            }

            await _context.Users.AddRangeAsync(users);
            _logger.LogInformation("Seeded {Count} users", users.Count);
        }

        private async Task SeedRolesAsync()
        {
            // Roles are added directly to users in the SeedUsersAsync method
            // This method could be used to seed a separate Roles table if needed
            await Task.CompletedTask;
            _logger.LogInformation("Roles seeded with users");
        }


    }
}
