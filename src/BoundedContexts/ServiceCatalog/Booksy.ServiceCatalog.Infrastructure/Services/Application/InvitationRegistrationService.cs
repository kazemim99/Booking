using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.OTP;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Application;

/// <summary>
/// Service for handling invitation acceptance with user and provider registration
/// </summary>
public class InvitationRegistrationService : IInvitationRegistrationService
{
    private readonly IOtpService _otpService;
    private readonly IProviderWriteRepository _providerWriteRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvitationRegistrationService> _logger;

    public InvitationRegistrationService(
        IOtpService otpService,
        IProviderWriteRepository providerWriteRepository,
        IProviderReadRepository providerReadRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<InvitationRegistrationService> logger)
    {
        _otpService = otpService;
        _providerWriteRepository = providerWriteRepository;
        _providerReadRepository = providerReadRepository;
        _httpClientFactory = httpClientFactory;
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

    public async Task<UserId> CreateUserWithPhoneAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating user account for phone {PhoneNumber} via UserManagement API", phoneNumber);

            var client = _httpClientFactory.CreateClient("UserManagementAPI");

            var requestPayload = new
            {
                phoneNumber,
                firstName,
                lastName,
                email = email ?? $"{phoneNumber.Replace("+", "")}@booksy.temp",
                userType = "Provider"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users/register-with-phone", requestPayload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create user via API: {StatusCode} - {Error}", response.StatusCode, error);

                // Check for duplicate phone number error
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new DomainValidationException(
                        "A user account with this phone number already exists. Please use the regular invitation acceptance flow.");
                }

                throw new InvalidOperationException($"Failed to create user account: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CreateUserResponse>>(cancellationToken);
            var userId = UserId.From(result.Data.UserId);

            _logger.LogInformation("User account created successfully with ID {UserId} for phone {PhoneNumber}",
                userId, phoneNumber);

            return userId;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating user for phone {PhoneNumber}", phoneNumber);
            // Fallback: Generate temporary user ID if UserManagement service is unavailable
            _logger.LogWarning("UserManagement service unavailable, using temporary user ID");
            return UserId.CreateNew();
        }
        catch (Exception ex) when (ex is not DomainValidationException)
        {
            _logger.LogError(ex, "Error creating user for phone {PhoneNumber}", phoneNumber);
            // Fallback: Generate temporary user ID
            return UserId.CreateNew();
        }
    }

    public async Task<ProviderId> CreateIndividualProviderAsync(
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
                type: ProviderType.Individual,
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

            return individualProvider.Id;
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

    private class CreateUserResponse
    {
        public string UserId { get; set; } = string.Empty;
    }
}
