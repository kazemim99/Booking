using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Services;

namespace Booksy.Host.Composition;

/// <summary>
/// Seeds the demo salon's reviews (<see cref="DemoSalonReviewsSeeder"/>) by people who exist: the reviewers are created
/// in the person directory first, by phone, so the public listing names them the way it names anyone («مریم ر.»).
/// The host does it because it is the one project that reaches both contexts.
/// </summary>
/// <remarks>
/// Runs where <c>Database:SeedDemoReviews</c> is on, which defaults to <c>Database:SeedOnStartup</c> — Development, never
/// production. The reviewers' numbers are a block no seeded or test person uses; <c>GetOrCreateByPhoneAsync</c> finds
/// them again on the next start, so a re-run adds nobody. New people are flushed by the provisioning service itself,
/// and their domain events are never published — no welcome message goes anywhere.
/// </remarks>
public static class DemoSalonReviews
{
    /// <summary>The salon's reviewers: its customers are women, as a ladies' salon's are.</summary>
    public static readonly IReadOnlyList<(string FirstName, string LastName, string Phone)> Reviewers = new[]
    {
        ("مریم", "رضایی", "+989990001001"),
        ("سارا", "محمدی", "+989990001002"),
        ("نگار", "حسینی", "+989990001003"),
        ("زهرا", "کریمی", "+989990001004"),
        ("الهه", "موسوی", "+989990001005"),
        ("نیلوفر", "صادقی", "+989990001006"),
        ("پریسا", "احمدی", "+989990001007"),
        ("شیرین", "جعفری", "+989990001008"),
        ("مهسا", "نوری", "+989990001009"),
        ("فاطمه", "اکبری", "+989990001010"),
        ("سمانه", "یوسفی", "+989990001011"),
        ("ستاره", "باقری", "+989990001012"),
    };

    /// <summary>Seeds the salon's reviews unless it is missing or already has them. Returns how many were written.</summary>
    public static async Task<int> SeedAsync(
        IServiceProvider services,
        string salonName = DemoSalonReviewsSeeder.DemoSalonName,
        CancellationToken cancellationToken = default)
    {
        var seeder = new DemoSalonReviewsSeeder(
            services.GetRequiredService<ServiceCatalogDbContext>(),
            services.GetRequiredService<ILogger<DemoSalonReviewsSeeder>>());

        // Nobody is created for a salon that is not there, or that has its reviews already.
        if (await seeder.SalonToSeedAsync(salonName, cancellationToken) is null)
            return 0;

        var people = services.GetRequiredService<IPersonProvisioningService>();
        var reviewerIds = new List<Guid>(Reviewers.Count);
        foreach (var (first, last, phone) in Reviewers)
        {
            var result = await people.GetOrCreateByPhoneAsync(
                PhoneNumber.From(phone), UserType.Customer, first, last, cancellationToken: cancellationToken);
            reviewerIds.Add(result.Person.Id.Value);
        }

        return await seeder.SeedAsync(salonName, reviewerIds, cancellationToken);
    }
}
