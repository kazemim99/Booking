// AsanRezerve.UserManagement.Infrastructure/Persistence/UserManagementOutboxProcessor.cs
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.Persistence.Outbox;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Infrastructure.Persistence
{
    public class UserManagementOutboxProcessor : OutboxProcessor<UserManagementDbContext>
    {
        public UserManagementOutboxProcessor(
            UserManagementDbContext context,
            IDomainEventDispatcher eventBus,
            ILogger<UserManagementOutboxProcessor> logger)
            : base(context, eventBus, logger)
        {
        }
    }
}