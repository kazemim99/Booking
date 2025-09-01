// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Exceptions;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Commands.ActivateUser
{
    public sealed class DeActivateUserCommandHandler : ICommandHandler<DeActivateUserCommand, DeActivateUserResult>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly IUserReadRepository _userReadRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<DeActivateUserCommandHandler> _logger;

        public DeActivateUserCommandHandler(
            IUserWriteRepository userWriteRepository,
            IUserReadRepository userReadRepository,
            IUnitOfWork unitOfWork,
            IAuditUserService auditService,
            ILogger<DeActivateUserCommandHandler> logger)
        {
            _userWriteRepository = userWriteRepository;
            _userReadRepository = userReadRepository;
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
            var user = await _userReadRepository.GetByIdAsync(id, cancellationToken);

            if (user == null)
            {
                throw new InvalidUserProfileException("User not found");
            }

            user.Activate(request.ActivationToken);

            await _userWriteRepository.UpdateUserAsync(user, cancellationToken);
            //await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            await _auditService.LogActivationAsync(user.Id, cancellationToken);

            _logger.LogInformation("User activated successfully. UserId: {UserId}", user.Id);

            return new DeActivateUserResult(
                UserId: user.Id.Value,
                Email: user.Email.Value,
                ActivatedAt: user.ActivatedAt!.Value);
        }
    }
}
