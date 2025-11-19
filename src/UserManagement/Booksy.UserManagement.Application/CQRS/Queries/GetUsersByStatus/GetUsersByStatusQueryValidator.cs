// 📁 Booksy.UserManagement.Application/Queries/GetUsersByStatus/GetUsersByStatusQueryValidator.cs
using FluentValidation;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus
{
    /// <summary>
    /// Validator for GetUsersByStatusQuery
    /// </summary>
    public sealed class GetUsersByStatusQueryValidator : AbstractValidator<GetUsersByStatusQuery>
    {
        public GetUsersByStatusQueryValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid user status provided");

            RuleFor(x => x.MaxResults)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000)
                .WithMessage("MaxResults must be between 1 and 1000");
        }
    }
}