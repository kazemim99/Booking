// AsanRezerve.UserManagement.Infrastructure/Persistence/UserManagementUnitOfWork.cs
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.UserManagement.Application.Abstractions.Persistence;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

public class UserManagementUnitOfWork : EfCoreUnitOfWork<UserManagementDbContext>, IUserManagementUnitOfWork
{
    public UserManagementUnitOfWork(UserManagementDbContext context, ILogger<EfCoreUnitOfWork<UserManagementDbContext>> logger, IDomainEventDispatcher eventDispatcher)
        : base(context, logger,eventDispatcher)
    {
    }
}