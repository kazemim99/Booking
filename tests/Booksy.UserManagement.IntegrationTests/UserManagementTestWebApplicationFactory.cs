// ========================================
// Booksy.UserManagement.IntegrationTests/UserManagementTestWebApplicationFactory.cs
// ========================================
using Booksy.Core.Application.Services.Notifications;
using Booksy.Tests.Commons;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Booksy.UserManagement.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for User Management integration tests
/// Inherits from generic TestWebApplicationFactory and can add UserManagement-specific configuration
/// </summary>
public class UserManagementTestWebApplicationFactory<TStartup>
    : TestWebApplicationFactory<TStartup, UserManagementDbContext>
    where TStartup : class
{
    public UserManagementTestWebApplicationFactory()
        : base("UserManagement")
    {
    }

    /// <summary>
    /// Configure UserManagement-specific test services
    /// </summary>
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        // The base TestWebApplicationFactory sets ASPNETCORE_ENVIRONMENT to "Test", but
        // Booksy.UserManagement.API only ships appsettings.{Development,Production}.json -- no
        // appsettings.Test.json -- so this host falls back to the un-suffixed appsettings.json,
        // which is the PRODUCTION-shaped config (Rahyab:SandboxMode = false, no Sms:SandboxMode
        // key at all). Any handler that sends a real SMS (PhoneVerification-based OTP flows,
        // e.g. SendVerificationCodeCommandHandler / SendPhoneVerificationCodeCommandHandler)
        // would therefore try a genuine outbound call to a real SMS gateway from inside the test
        // sandbox -- which has no route out -- and fail with an SSL/connection error 90+ seconds
        // later instead of a fast, deterministic result. Replacing the port with a fake removes
        // the dependency on environment/config resolution entirely, which is more robust than
        // adding an appsettings.Test.json this factory might not even load consistently.
        services.RemoveAll<ISmsNotificationService>();
        services.AddSingleton<ISmsNotificationService, FakeSmsNotificationService>();
    }
}

/// <summary>
/// Always-succeeds, no-network <see cref="ISmsNotificationService"/> for integration tests.
/// Captures every sent message so a test can recover the OTP code from it -- the
/// <see cref="Domain.Aggregates.PhoneVerificationAggregate.PhoneVerification"/> aggregate itself
/// never persists the plaintext code (<c>PhoneVerificationConfiguration</c> explicitly
/// <c>.Ignore()</c>s <c>OtpCode</c>, storing only its SHA-256 hash), so re-reading the row from
/// the database after the request completes -- as a first version of this fake did -- gets back
/// a null <c>OtpCode</c>, not the code that was actually sent.
/// </summary>
public sealed class FakeSmsNotificationService : ISmsNotificationService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _lastMessageByPhone = new();

    /// <summary>The most recent message sent to this phone number (E.164 or as passed), or null if none.</summary>
    public string? LastMessageTo(string phoneNumber) =>
        _lastMessageByPhone.TryGetValue(phoneNumber, out var message) ? message : null;

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
        string phoneNumber,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _lastMessageByPhone[phoneNumber] = message;
        return Task.FromResult<(bool, string?, string?)>((true, $"fake-{Guid.NewGuid():N}", null));
    }

    public Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
        List<string> phoneNumbers,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var phone in phoneNumbers)
        {
            _lastMessageByPhone[phone] = message;
        }

        return Task.FromResult(phoneNumbers
            .Select(p => (p, true, (string?)$"fake-{Guid.NewGuid():N}", (string?)null))
            .ToList());
    }
}
