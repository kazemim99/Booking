using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.UserManagement.Application.Abstractions.Queries;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;

namespace AsanRezerve.UserManagement.Infrastructure.Queries;

public class UserQueryRepository : QueryRepositoryBase<User,UserId> ,IUserQueryRepository
{
    private readonly UserManagementDbContext _context;

    public UserQueryRepository(UserManagementDbContext context):base(context)
    {
        _context = context;
    }

}

