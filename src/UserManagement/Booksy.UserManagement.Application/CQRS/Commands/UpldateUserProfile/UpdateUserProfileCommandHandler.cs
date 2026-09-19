using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.Abstractions.Persistence;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.UpldateUserProfile
{
    /// <summary>
    /// Lets a person name themselves. Phone sign-in gives every account a placeholder
    /// («ارائه‌دهنده 9123135143»), and that is the name colleagues and customers see in staff
    /// pickers and bookings — so this is how it gets corrected.
    ///
    /// <para>The phone number is deliberately NOT changed here: it is the sign-in identity and has
    /// its own verified flow. A request that carries a different number is refused rather than
    /// silently ignored.</para>
    /// </summary>
    public sealed class UpdateUserProfileCommandHandler
        : ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResult>
    {
        private readonly IUserRepository _users;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

        public UpdateUserProfileCommandHandler(
            IUserRepository users,
            IUserManagementUnitOfWork unitOfWork,
            ILogger<UpdateUserProfileCommandHandler> logger)
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UpdateUserProfileResult> Handle(
            UpdateUserProfileCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(UserId.From(request.Id), cancellationToken)
                ?? throw new NotFoundException("User not found");

            var firstName = request.FirstName?.Trim();
            var lastName = request.LastName?.Trim();
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainValidationException(nameof(request.FirstName), "نام الزامی است");

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
                !SamePhone(request.PhoneNumber!, user.Profile.PhoneNumber?.Value))
            {
                throw new DomainValidationException(
                    nameof(request.PhoneNumber),
                    "تغییر شماره موبایل از این مسیر ممکن نیست");
            }

            user.Profile.UpdateName(firstName, lastName ?? string.Empty, null);
            if (request.Bio != null)
                user.Profile.UpdateBio(request.Bio);

            await _users.UpdateAsync(user, cancellationToken);
            // The pipeline's TransactionBehavior commits the ServiceCatalog context (DI last-wins),
            // so this context is committed here or the change is silently discarded.
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Profile name updated for user {UserId}", user.Id.Value);
            return new UpdateUserProfileResult();
        }

        /// <summary>Any spelling of the same number counts as unchanged.</summary>
        private static bool SamePhone(string requested, string? current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            try
            {
                return PhoneNumber.From(requested).Value == PhoneNumber.From(current).Value;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
