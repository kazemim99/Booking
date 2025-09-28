using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.UserManagement.Application.Abstractions.Queries;
using Booksy.UserManagement.Infrastructure.Persistence.Context;

namespace Booksy.UserManagement.Infrastructure.Queries;

public class UserQueryRepository : QueryRepositoryBase<User,UserId> ,IUserQueryRepository
{
    private readonly UserManagementDbContext _context;

    public UserQueryRepository(UserManagementDbContext context):base(context)
    {
        _context = context;
    }

}

