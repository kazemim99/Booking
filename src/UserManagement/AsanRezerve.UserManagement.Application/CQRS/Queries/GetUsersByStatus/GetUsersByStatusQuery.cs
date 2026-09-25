// 📁 AsanRezerve.UserManagement.Application/Queries/GetUsersByStatus/GetUsersByStatusQuery.cs
using AsanRezerve.UserManagement.Domain.Enums;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    public sealed record GetUsersByStatusQuery(
        UserStatus Status,
        int MaxResults = 100) : IQuery<IReadOnlyList<GetUsersByStatusResult>>;
}