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
    private readonly IPersonProvisioningService _personProvisioningService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly Abstractions.Persistence.IUserManagementUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteCustomerAuthenticationCommandHandler> _logger;

    public CompleteCustomerAuthenticationCommandHandler(
        ISender mediator,
        IPhoneVerificationRepository verificationRepo,
        IUserRepository userRepository,
        IPersonProvisioningService personProvisioningService,
        ICustomerRepository customerRepository,
        IJwtTokenService jwtTokenService,
        Abstractions.Persistence.IUserManagementUnitOfWork unitOfWork,
        ILogger<CompleteCustomerAuthenticationCommandHandler> logger)
    {
        _mediator = mediator;
        _verificationRepo = verificationRepo;
        _userRepository = userRepository;
        _personProvisioningService = personProvisioningService;
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
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

        // Step 2: Resolve the Person for this phone through the single guarded path.
        // ONE PERSON PER PHONE: an existing account is reused and granted the Customer
        // capacity (a provider booking as a customer becomes Type=Both) instead of
        // being rejected — which previously created a second account for one human.
        var provisioning = await _personProvisioningService.GetOrCreateByPhoneAsync(
            phoneNumber,
            UserType.Customer,
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);

        var user = provisioning.Person;
        var isNewUser = provisioning.IsNewPerson;
        // A Customer aggregate is needed whenever this person is newly able to act
        // as a customer — a brand-new account or an existing provider gaining it.
        var isNewCustomer = provisioning.IsNewPerson || provisioning.CapacityGranted;

        _logger.LogInformation(
            "Resolved customer person {UserId} (new: {IsNew}, capacity granted: {Granted})",
            user.Id.Value, isNewUser, provisioning.CapacityGranted);

        // Blocked/inactive accounts must not obtain tokens via OTP. The password
        // path enforces this in User.Authenticate(), but the OTP flow issues tokens
        // directly and would otherwise bypass it. Deleted users never reach here —
        // the DbContext query filter excludes them.
        if (user.Status is UserStatus.Banned or UserStatus.Suspended or UserStatus.Inactive)
        {
            _logger.LogWarning(
                "Rejected OTP sign-in for {Status} customer account. UserId: {UserId}",
                user.Status, user.Id.Value);

            throw new InvalidOperationException(
                $"This account is {user.Status} and cannot sign in. Please contact support.");
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
        // Persist explicitly (SaveAsync only tracks; the generic TransactionBehavior
        // commits the ServiceCatalog UoW, not UserManagement) so the new customer + their
        // Customer aggregate are actually saved.
        await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

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
    /// Masks phone number for logging (shows only last 4 digits)
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        return $"****{phoneNumber[^4..]}";
    }
}
