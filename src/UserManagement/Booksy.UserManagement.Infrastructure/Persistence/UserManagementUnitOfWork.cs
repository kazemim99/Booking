// Booksy.UserManagement.Infrastructure/Persistence/UserManagementUnitOfWork.cs
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

public class UserManagementUnitOfWork : EfCoreUnitOfWork<UserManagementDbContext>
{
    public UserManagementUnitOfWork(UserManagementDbContext context, ILogger<EfCoreUnitOfWork<UserManagementDbContext>> logger, IDomainEventDispatcher eventDispatcher)
        : base(context, logger,eventDispatcher)
    {
    }
}