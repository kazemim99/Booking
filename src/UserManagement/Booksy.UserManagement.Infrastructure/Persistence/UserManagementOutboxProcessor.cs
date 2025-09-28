// Booksy.UserManagement.Infrastructure/Persistence/UserManagementOutboxProcessor.cs
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Outbox;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Persistence
{
    public class UserManagementOutboxProcessor : OutboxProcessor<UserManagementDbContext>
    {
        public UserManagementOutboxProcessor(
            UserManagementDbContext context,
            IEventBus eventBus,
            ILogger<UserManagementOutboxProcessor> logger)
            : base(context, eventBus, logger)
        {
        }
    }
}