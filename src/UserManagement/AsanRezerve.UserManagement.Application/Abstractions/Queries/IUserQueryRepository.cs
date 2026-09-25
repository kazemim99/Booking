using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsanRezerve.UserManagement.Application.Abstractions.Queries
{
    public interface IUserQueryRepository: IQueryRepositoryBase<User,UserId>
    {
    }
}
