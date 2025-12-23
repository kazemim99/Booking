using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds sample staff members to providers
    /// </summary>
    public sealed class StaffDataSeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<StaffDataSeeder> _logger;

        public StaffDataSeeder(
            ServiceCatalogDbContext context,
            ILogger<StaffDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get providers that don't have staff yet
                var providers = await _context.Providers
                    .Include(p => p.Staff)
                    .Where(p => !p.Staff.Any())
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogInformation("All providers already have staff. Skipping...");
                    return;
                }

                _logger.LogInformation("Adding staff to {Count} providers", providers.Count);

                var totalStaffAdded = 0;

                foreach (var provider in providers)
                {
                    var staffCount = AddStaffToProvider(provider);
                    totalStaffAdded += staffCount;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added {TotalStaff} staff members to {ProviderCount} providers",
                    totalStaffAdded, providers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding staff data");
                throw;
            }
        }

        private int AddStaffToProvider(Domain.Aggregates.Provider provider)
        {
            var staffCount = 0;

            switch (provider.PrimaryCategory)
            {
                case ServiceCategory.Barbershop:
                    // Individual has 1-2 staff
                    provider.AddStaff("John", "Smith", StaffRole.Owner, PhoneNumber.From("5551234567"));
                    staffCount = 1;
                    break;

                case ServiceCategory.HairSalon:
                case ServiceCategory.BeautySalon:
                case ServiceCategory.NailSalon:
                    // Salon has 3-5 staff
                    provider.AddStaff("Sarah", "Johnson", StaffRole.Owner, PhoneNumber.From("5551111111"));
                    provider.AddStaff("Emily", "Davis", StaffRole.ServiceProvider, PhoneNumber.From("5552222222"));
                    provider.AddStaff("Michael", "Brown", StaffRole.ServiceProvider, PhoneNumber.From("5553333333"));
                    provider.AddStaff("Jessica", "Wilson", StaffRole.Assistant, PhoneNumber.From("5554444444"));
                    staffCount = 4;
                    break;

                case ServiceCategory.Spa:
                case ServiceCategory.Massage:
                    // Spa has 4-6 staff
                    provider.AddStaff("Amanda", "Martinez", StaffRole.Owner, PhoneNumber.From("5555555555"));
                    provider.AddStaff("David", "Garcia", StaffRole.ServiceProvider, PhoneNumber.From("5556666666"));
                    provider.AddStaff("Lisa", "Rodriguez", StaffRole.ServiceProvider, PhoneNumber.From("5557777777"));
                    provider.AddStaff("Christopher", "Lee", StaffRole.ServiceProvider, PhoneNumber.From("5558888888"));
                    provider.AddStaff("Jennifer", "Walker", StaffRole.Receptionist, PhoneNumber.From("5559999999"));
                    staffCount = 5;
                    break;

                case ProviderType.Medical:
                    // Medical facility has specialized staff
                    provider.AddStaff("Dr. Robert", "Chen", StaffRole.Specialist, PhoneNumber.From("5550000000"));
                    provider.AddStaff("Nurse Maria", "Lopez", StaffRole.ServiceProvider, PhoneNumber.From("5550000001"));
                    provider.AddStaff("Anna", "Kim", StaffRole.Assistant, PhoneNumber.From("5550000002"));
                    staffCount = 3;
                    break;

                case ProviderType.Clinic:
                    // Clinic has medical staff
                    provider.AddStaff("Dr. James", "Anderson", StaffRole.Specialist, PhoneNumber.From("5550000003"));
                    provider.AddStaff("Nurse Patricia", "Thomas", StaffRole.ServiceProvider, PhoneNumber.From("5550000004"));
                    staffCount = 2;
                    break;

                case ProviderType.GymFitness:
                    // Fitness center has trainers
                    provider.AddStaff("Coach Mike", "Thompson", StaffRole.Owner, PhoneNumber.From("5550000005"));
                    provider.AddStaff("Trainer Alex", "White", StaffRole.ServiceProvider, PhoneNumber.From("5550000006"));
                    provider.AddStaff("Instructor Sam", "Harris", StaffRole.ServiceProvider, PhoneNumber.From("5550000007"));
                    staffCount = 3;
                    break;

                case ProviderType.Professional:
                    // Professional service center has diverse staff
                    provider.AddStaff("Dr. Emma", "Clark", StaffRole.Owner, PhoneNumber.From("5550000008"));
                    provider.AddStaff("Therapist Olivia", "Lewis", StaffRole.ServiceProvider, PhoneNumber.From("5550000009"));
                    provider.AddStaff("Counselor Daniel", "Young", StaffRole.Specialist, PhoneNumber.From("5550000010"));
                    staffCount = 3;
                    break;

                default:
                    // Default: add owner and one staff
                    provider.AddStaff("Owner", "Default", StaffRole.Owner, PhoneNumber.From("5559999998"));
                    staffCount = 1;
                    break;
            }

            return staffCount;
        }
    }
}
