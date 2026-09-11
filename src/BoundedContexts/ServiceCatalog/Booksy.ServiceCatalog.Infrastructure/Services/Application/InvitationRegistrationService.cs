using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.OTP;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Application;

/// <summary>
/// Service for handling invitation acceptance with user and provider registration
/// </summary>
public class InvitationRegistrationService : IInvitationRegistrationService
{
    private readonly IOtpService _otpService;
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IPersonAccountProvisioningService _accountProvisioning;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvitationRegistrationService> _logger;

    public InvitationRegistrationService(
        IOtpService otpService,
        IProviderWriteRepository providerWriteRepository,
        IProviderReadRepository providerReadRepository,
        IPersonAccountProvisioningService accountProvisioning,
        IConfiguration configuration,
        ILogger<InvitationRegistrationService> logger)
    {
        _otpService = otpService;
        _providerWriteRepository = providerWriteRepository;
        _providerReadRepository = providerReadRepository;
        _accountProvisioning = accountProvisioning;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying OTP for phone number {PhoneNumber}", phoneNumber);

            var result = _otpService.VerifyCode(phoneNumber, otpCode);

            if (result.Matched)
            {
                _logger.LogInformation("OTP verification successful for {PhoneNumber}", phoneNumber);
                return true;
            }

            _logger.LogWarning("OTP verification failed for {PhoneNumber}", phoneNumber);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public Task<string> GenerateOtpCodeAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        // TOTP: deterministic for (secret + phoneNumber, current time window) -- nothing to
        // persist, and VerifyOtpAsync (above) accepts exactly this value for the same phone
        // within the same window.
        return Task.FromResult(_otpService.GetCode(phoneNumber));
    }

    public async Task<CreatedPersonAccount> CreateUserWithPhoneAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        // In-process (see IPersonAccountProvisioningService): this used to POST
        // /api/v1/users over a loopback HttpClient, which routes through the canonical
        // guarded account-creation path anyway (PersonProvisioningService) but paid for a
        // network round trip -- and one that a transient failure here has no retry story
        // for, unlike a direct in-process call sharing this request's ambient failure
        // handling.
        try
        {
            _logger.LogInformation("Creating user account for phone {PhoneNumber}", phoneNumber);

            var created = await _accountProvisioning.CreateWithPhoneAsync(
                phoneNumber, firstName, lastName, email, cancellationToken);

            var userId = UserId.From(created.PersonId);
            _logger.LogInformation(
                "User account {Outcome} with ID {UserId} for phone {PhoneNumber}",
                created.IsNewAccount ? "created" : "reused (registered concurrently)", userId, phoneNumber);

            return new CreatedPersonAccount(userId, created.IsNewAccount);
        }
        catch (Exception ex) when (ex is not DomainValidationException)
        {
            // Do NOT fabricate a UserId here: returning UserId.CreateNew() would let the
            // caller build a membership around a person that has no users row (an orphan
            // that can never authenticate, and the caller's compensation never runs since
            // no exception propagated). Fail hard so the transaction rolls back instead.
            _logger.LogError(ex, "Error creating user for phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }
}
