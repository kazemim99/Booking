// ========================================
// Booksy.UserManagement.Application/Commands/RegisterUser/RegisterUserCommand.cs
// ========================================



using Booksy.Core.Application.Services;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Exceptions;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.CQRS.Commands.RegisterUser
{
    /// <summary>
    /// Handler for RegisterUserCommand
    /// </summary>
    public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;        
        private readonly IUserValidationService _validationService;
        private readonly IEmailTemplateService _emailService;
        private readonly IReferralService _referralService;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IUserRepository userWriteRepository,
            IUserValidationService validationService,
            IEmailTemplateService emailService,
            IReferralService referralService,
            IAuditUserService auditService,
            ILogger<RegisterUserCommandHandler> logger,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userWriteRepository;

            _validationService = validationService;
            _emailService = emailService;
            _referralService = referralService;
            _auditService = auditService;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterUserResult> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing user registration for email: {Email}", request.Email);

                // Validate email availability
                var email = Email.From(request.Email);
                var emailExists = await _validationService.IsEmailAvailableAsync(email, cancellationToken);

                if (emailExists)
                {
                    throw new UserAlreadyExistsException(email);
                }

                var hashedPassword =  _passwordHasher.HashPassword(request.Password);
                var password = HashedPassword.Create(hashedPassword);


                // Create user profile
                var profile = UserProfile.Create(
                    request.FirstName,
                    request.LastName,
                    request.MiddleName,
                    request.DateOfBirth,
                    request.Gender);

                // Set contact information
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var phoneNumber = PhoneNumber.From(request.PhoneNumber);
                    Address? address = null;

                    if (request.Address != null)
                    {
                        address = Address.Create(
                            request.Address.Street,
                            request.Address.City,
                            request.Address.State,
                            request.Address.PostalCode,
                            request.Address.Country,
                            request.Address.Unit);
                    }

                    profile.UpdateContactInfo(phoneNumber, null, address);
                }

                // Set additional profile information
                if (!string.IsNullOrWhiteSpace(request.Bio))
                {
                    profile.UpdateBio(request.Bio);
                }

                if (!string.IsNullOrWhiteSpace(request.PreferredLanguage) ||
                    !string.IsNullOrWhiteSpace(request.TimeZone))
                {
                    profile.UpdateLocalizationSettings(request.PreferredLanguage, request.TimeZone);
                }

                // Set preferences
                if (request.Preferences != null)
                {
                    foreach (var preference in request.Preferences)
                    {
                        profile.SetPreference(preference.Key, preference.Value);
                    }
                }


                // Register user
                var user = User.Register(email, password, profile, request.UserType);

                // Handle referral if provided
                if (!string.IsNullOrWhiteSpace(request.ReferralCode))
                {
                    await ProcessReferralAsync(request.ReferralCode, user.Id, cancellationToken);
                }

                // Persist user
                await _userRepository.SaveAsync(user, cancellationToken);

                // Send welcome email if requested
                if (request.SendWelcomeEmail)
                {
                    await SendWelcomeEmailAsync(user, cancellationToken);
                }

                // Create audit entry
                await _auditService.LogRegistrationAsync(
                    user.Id,
                    request.IpAddress,
                    request.UserAgent,
                    cancellationToken);

                _logger.LogInformation(
                    "User registered successfully. UserId: {UserId}, Email: {Email}",
                    user.Id,
                    user.Email);

                return new RegisterUserResult(
                    UserId: user.Id.Value,
                    Email: user.Email.Value,
                    FullName: profile.GetFullName(),
                    Status: user.Status,
                    RequiresActivation: request.RequireEmailVerification,
                    ActivationToken: user.ActivationToken?.Token,
                    TokenExpiresAt: user.ActivationToken?.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register user with email: {Email}", request.Email);
                throw;
            }
        }

        private async Task ProcessReferralAsync(
            string referralCode,
            UserId newUserId,
            CancellationToken cancellationToken)
        {
            try
            {
                var referrerId = await _referralService.GetReferrerIdAsync(referralCode, cancellationToken);
                if (referrerId != null)
                {
                    await _referralService.RecordReferralAsync(
                        referrerId.Value,
                        newUserId,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process referral code: {ReferralCode}", referralCode);
                // Don't fail registration due to referral issues
            }
        }

        private async Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                var emailData = new Dictionary<string, string>
                {
                    ["FirstName"] = user.Profile.FirstName,
                    ["LastName"] = user.Profile.LastName,
                    ["Email"] = user.Email.Value,
                    ["ActivationToken"] = user.ActivationToken?.Token ?? string.Empty,
                    ["ActivationUrl"] = $"https://booksy.com/activate?token={user.ActivationToken?.Token}"
                };

                await _emailService.SendEmailAsync(
                    user.Email.Value,
                    EmailTemplate.Templates.Welcome,
                    emailData,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to: {Email}", user.Email);
                // Don't fail registration due to email issues
            }
        }
    }
}

