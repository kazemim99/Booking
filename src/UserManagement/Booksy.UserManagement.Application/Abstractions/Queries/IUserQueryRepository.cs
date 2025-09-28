using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.UserManagement.Application.Abstractions.Queries
{
    public interface IUserQueryRepository: IQueryRepositoryBase<User,UserId>
    {
    }
}
