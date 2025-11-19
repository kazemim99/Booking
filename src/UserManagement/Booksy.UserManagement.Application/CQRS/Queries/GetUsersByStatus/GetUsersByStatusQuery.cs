// 📁 Booksy.UserManagement.Application/Queries/GetUsersByStatus/GetUsersByStatusQuery.cs
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    public sealed record GetUsersByStatusQuery(
        UserStatus Status,
        int MaxResults = 100) : IQuery<IReadOnlyList<GetUsersByStatusResult>>;
}