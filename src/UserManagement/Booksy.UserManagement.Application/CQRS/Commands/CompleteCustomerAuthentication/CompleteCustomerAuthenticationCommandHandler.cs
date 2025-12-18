// ========================================
// CompleteCustomerAuthenticationCommandHandler.cs
// Unified handler for customer phone verification and authentication
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.CQRS.Commands.VerifyPhone;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using MediatR;

namespace Booksy.UserManagement.Application.CQRS.Commands.CompleteCustomerAuthentication;

/// <summary>
/// Handler for completing customer authentication via phone verification
/// Supports both new customer registration and existing customer login
/// </summary>
public sealed class CompleteCustomerAuthenticationCommandHandler
    : ICommandHandler<CompleteCustomerAuthenticationCommand, CompleteCustomerAuthenticationResponse>
{
    private readonly ISender _mediator;
    private readonly IPhoneVerificationRepository _verificationRepo;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<CompleteCustomerAuthenticationCommandHandler> _logger;

    public CompleteCustomerAuthenticationCommandHandler(
        ISender mediator,
        IPhoneVerificationRepository verificationRepo,
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IJwtTokenService jwtTokenService,
        ILogger<CompleteCustomerAuthenticationCommandHandler> logger)
    {
        _mediator = mediator;
        _verificationRepo = verificationRepo;
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<CompleteCustomerAuthenticationResponse> Handle(
        CompleteCustomerAuthenticationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting customer authentication for phone: {Phone}",
            MaskPhoneNumber(request.PhoneNumber));

        // Step 1: Verify the phone number and code
        var phoneNumber = PhoneNumber.From(request.PhoneNumber);
        var verification = await _verificationRepo.GetByPhoneNumberAsync(phoneNumber, cancellationToken);

        if (verification == null)
        {
            throw new InvalidOperationException("No verification found for this phone number");
        }

        // Verify the code using existing command
        var verifyCommand = new VerifyPhoneCommand(
            VerificationId: verification.Id.Value,
            Code: request.Code);

        var verifyResult = await _mediator.Send(verifyCommand, cancellationToken);

        if (!verifyResult.Success)
        {
            throw new InvalidOperationException(verifyResult.Message);
        }

        _logger.LogInformation("Phone verification successful for: {Phone}", MaskPhoneNumber(request.PhoneNumber));

        // Step 2: Look up or create User
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber.Value, cancellationToken);
        bool isNewUser = false;
        bool isNewCustomer = false;

        if (user == null)
        {
            // Create new user for customer
            user = await CreateNewCustomerUser(request, phoneNumber, cancellationToken);
            isNewUser = true;
            isNewCustomer = true;

            _logger.LogInformation(
                "Created new customer user. UserId: {UserId}",
                user.Id.Value);
        }
        else
        {
            // Validate user type
            if (user.Type != UserType.Customer)
            {
                throw new InvalidOperationException(
                    $"This phone number is registered as {user.Type}. Please use the appropriate login endpoint.");
            }

            _logger.LogInformation(
                "Existing customer user found. UserId: {UserId}",
                user.Id.Value);
        }

        // Step 3: Ensure Customer aggregate exists
        var customer = await _customerRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (customer == null)
        {
            customer = Domain.Aggregates.CustomerAggregate.Customer.Create(user.Id, user.Profile);
            await _customerRepository.SaveAsync(customer, cancellationToken);
            isNewCustomer = true;

            _logger.LogInformation(
                "Created Customer aggregate. CustomerId: {CustomerId}, UserId: {UserId}",
                customer.Id.Value,
                user.Id.Value);
        }

        // Step 4: Generate authentication tokens
        var roles = user.Roles.Select(r => r.Name).ToList();
        var displayName = user.Profile.GetFullName();

        var accessToken = _jwtTokenService.GenerateAccessToken(
            userId: user.Id,
            userType: UserType.Customer,
            email: user.Email,
            displayName: displayName,
            firstName: user.Profile.FirstName,
            lastName: user.Profile.LastName,
            status: user.Status.ToString(),
            roles: roles,
            providerId: null,
            providerStatus: null,
            customerId: customer.Id.Value.ToString(),
            phoneNumber: user.PhoneNumber?.Value,
            expirationHours: 24
        );

        // Generate refresh token
        var refreshToken = Domain.Entities.RefreshToken.Generate(
            expirationDays: 30,
            createdByIp: request.IpAddress);

        user.AddRefreshToken(refreshToken);
        await _userRepository.SaveAsync(user, cancellationToken);

        _logger.LogInformation(
            "Customer authentication completed successfully. UserId: {UserId}, CustomerId: {CustomerId}, IsNew: {IsNew}",
            user.Id.Value,
            customer.Id.Value,
            isNewCustomer);

        return new CompleteCustomerAuthenticationResponse
        {
            IsNewCustomer = isNewCustomer,
            UserId = user.Id.Value,
            CustomerId = customer.Id.Value,
            PhoneNumber = phoneNumber.Value,
            Email = user.Email.Value,
            FullName = displayName,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 86400, // 24 hours
            TokenType = "Bearer",
            Message = isNewCustomer
                ? "Welcome! Your account has been created successfully."
                : "Welcome back! You're now logged in."
        };
    }

    /// <summary>
    /// Creates a new User for a customer registration
    /// </summary>
    private async Task<User> CreateNewCustomerUser(
        CompleteCustomerAuthenticationCommand request,
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken)
    {
        // Use provided names or defaults
        var firstName = request.FirstName ?? "مهمان"; // "Customer" in Persian
        var lastName = request.LastName ?? phoneNumber.NationalNumber;

        // Create user profile
        var profile = UserProfile.Create(
            firstName,
            lastName,
            middleName: null,
            dateOfBirth: null,
            gender: null);

        // Create email (use provided or temp)
        Email email;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            email = Email.Create(request.Email);
        }
        else
        {
            // Temporary email: phone@booksy.customer
            email = Email.Create($"{phoneNumber.NationalNumber}@booksy.customer");
        }

        profile.UpdateContactInfo(phoneNumber, null, null);

        // Register user with phone (passwordless)
        var user = User.RegisterWithPhone(
            email,
            phoneNumber,
            profile,
            UserType.Customer);


        return user;
    }

    /// <summary>
    /// Masks phone number for logging (shows only last 4 digits)
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        return $"****{phoneNumber[^4..]}";
    }
}
