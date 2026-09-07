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

    public async Task<UserId> CreateUserWithPhoneAsync(
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

            var personId = await _accountProvisioning.CreateWithPhoneAsync(
                phoneNumber, firstName, lastName, email, cancellationToken);

            var userId = UserId.From(personId);
            _logger.LogInformation("User account created successfully with ID {UserId} for phone {PhoneNumber}",
                userId, phoneNumber);

            return userId;
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

    public async Task<Provider> CreateIndividualProviderAsync(
        UserId userId,
        string firstName,
        string lastName,
        string phoneNumber,
        string? email,
        ProviderId organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating individual provider for user {UserId} linked to organization {OrgId}",
                userId, organizationId);

            // Get organization to copy some basic settings
            var organization = await _providerReadRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                throw new NotFoundException($"Organization with ID {organizationId} not found");
            }

            // Create contact info
            var primaryPhone = PhoneNumber.From(phoneNumber);
            var userEmail = string.IsNullOrWhiteSpace(email)
                ? Email.Create($"{phoneNumber.Replace("+", "")}@booksy.temp")
                : Email.Create(email);

            var contactInfo = ContactInfo.Create(userEmail, primaryPhone);

            // Create new address based on organization address or use default
            // IMPORTANT: Don't reuse organization.Address as it's tracked by EF Core with different ProviderId
            var address = organization.Address != null
                ? BusinessAddress.Create(
                    formattedAddress: organization.Address.FormattedAddress,
                    street: organization.Address.Street,
                    city: organization.Address.City,
                    state: organization.Address.State,
                    postalCode: organization.Address.PostalCode,
                    country: organization.Address.Country,
                    latitude:organization.Address.Latitude,
                    longitude: organization.Address.Longitude)
                : BusinessAddress.Create(
                    formattedAddress: "Tehran, Iran",
                    street: "Main Street",
                    city: "Tehran",
                    state: "Tehran",
                    postalCode: "00000",
                    country: "Iran");

            // Create individual provider profile
            var individualProvider = Provider.CreateDraft(
                ownerId: userId,
                ownerFirstName: firstName,
                ownerLastName: lastName,
                businessName: $"{firstName} {lastName}", // Individual's name as business name
                description: $"Staff member at {organization.Profile.BusinessName}",
                primaryCategory: organization.PrimaryCategory,
                contactInfo: contactInfo,
                address: address,
                hierarchyType: ProviderHierarchyType.Individual,
                registrationStep: 9, // Completed registration
                logoUrl: null);

            // Link to organization
            individualProvider.LinkToOrganization(organizationId);

            // Activate the provider immediately
            individualProvider.Activate();
            individualProvider.CompleteRegistration();

            await _providerWriteRepository.SaveProviderAsync(individualProvider, cancellationToken);

            _logger.LogInformation(
                "Individual provider {ProviderId} created and linked to organization {OrgId}",
                individualProvider.Id, organizationId);

            return individualProvider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating individual provider for user {UserId} and organization {OrgId}",
                userId, organizationId);
            throw;
        }
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateAuthTokensAsync(
        UserId userId,
        ProviderId providerId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating auth tokens for user {UserId}, provider {ProviderId}",
                userId, providerId);

            // Generate JWT tokens using local implementation
            var issuer = _configuration["Jwt:Issuer"] ?? "Booksy";
            var audience = _configuration["Jwt:Audience"] ?? "Booksy.Users";
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, displayName),
                new("user_type", "Provider"),
                new("user-status", "Active"),
                new("providerId", providerId.Value.ToString()),
                new("provider_status", "Active"),
                new(ClaimTypes.Role, "Provider"),
                new(ClaimTypes.Role, "Staff"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Access token (24 hours)
            var accessToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            // Refresh token (30 days)
            var refreshToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessTokenString = tokenHandler.WriteToken(accessToken);
            var refreshTokenString = tokenHandler.WriteToken(refreshToken);

            _logger.LogInformation("Auth tokens generated successfully for user {UserId}", userId);

            return (accessTokenString, refreshTokenString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating auth tokens for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Compensation: Deletes a user account if registration flow fails
    /// This is part of the saga pattern for handling distributed transactions
    /// </summary>
    public async Task<bool> DeleteUserAsync(
        UserId userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        // In-process (see IPersonAccountProvisioningService). This used to DELETE
        // /api/v1/users/{id} over a loopback HttpClient -- an endpoint that (a) requires the
        // SysAdminOnly policy, which this unauthenticated-flow caller was never going to
        // satisfy, and (b) has no MediatR handler registered for DeleteUserCommand at all, so
        // the call could only ever 401 or 500. Both were swallowed by the try/catch below and
        // logged as a warning, so this compensation has been silently failing on every
        // register-and-accept that created an account and then failed a later step --
        // orphaning that account (and permanently claiming its phone number, since only a
        // Deleted-status row is excluded from the uniqueness checks) with no error surfaced
        // anywhere. The in-process path performs the same soft-delete those checks expect.
        try
        {
            _logger.LogWarning(
                "COMPENSATION: Deleting user {UserId} due to registration failure. Reason: {Reason}",
                userId, reason);

            var deleted = await _accountProvisioning.DeleteAsync(userId.Value, reason, cancellationToken);

            if (deleted)
            {
                _logger.LogInformation(
                    "COMPENSATION SUCCESS: User {UserId} deleted successfully",
                    userId);
            }
            else
            {
                _logger.LogWarning(
                    "COMPENSATION PARTIAL FAILURE: Failed to delete user {UserId}. Manual cleanup may be required.",
                    userId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - compensation is best effort
            _logger.LogError(ex,
                "COMPENSATION ERROR: Exception while deleting user {UserId}. Manual cleanup may be required.",
                userId);
            return false;
        }
    }
}
