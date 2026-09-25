// ========================================
// AsanRezerve.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Commands.DeActivateUser
{
    public sealed class DeActivateUserCommandHandler : ICommandHandler<DeActivateUserCommand, DeActivateUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<DeActivateUserCommandHandler> _logger;

        public DeActivateUserCommandHandler(
            IUserRepository userWriteRepository,
            IUserManagementUnitOfWork unitOfWork,
            IAuditUserService auditService,
            ILogger<DeActivateUserCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<DeActivateUserResult> Handle(
            DeActivateUserCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Activating user account for Id: {Id}", request.id);

            var id = UserId.From(request.id);
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);

            if (user == null)
            {
                throw new InvalidUserProfileException("User not found");
            }

            user.Activate(request.ActivationToken);

            await _userRepository.UpdateAsync(user, cancellationToken);
            // Commit the UserManagement unit of work explicitly. The pipeline's TransactionBehavior
            // commits the ServiceCatalog context (registered last, DI last-wins), so without this the
            // change was tracked and then silently discarded at the end of the request.
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            await _auditService.LogActivationAsync(user.Id, cancellationToken);

            _logger.LogInformation("User activated successfully. UserId: {UserId}", user.Id);

            return new DeActivateUserResult(
                UserId: user.Id.Value,
                Email: user.Email.Value,
                ActivatedAt: user.ActivatedAt!.Value);
        }
    }
}
