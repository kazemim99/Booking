// ========================================
// Booksy.UserManagement.Application/Queries/SearchUsers/SearchUsersQueryValidator.cs
// ========================================
using FluentValidation;
using Booksy.UserManagement.Domain.Enums;
using Booksy.Core.Application.Validators;

namespace Booksy.UserManagement.Application.CQRS.Queries.SearchUsers;

/// <summary>
/// Simplified validator for SearchUsersQuery
/// </summary>
public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    private static readonly HashSet<string> ValidSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Email", "FirstName", "LastName", "RegisteredAt", "ActivatedAt", "LastLoginAt", "Status", "Type"
    };

    public SearchUsersQueryValidator()
    {
        // Include pagination validation (reusable!)
        //Include(new PaginationRequestValidator());

        // Business-specific validations only
        RuleFor(x => x.SearchTerm)
            .MinimumLength(2)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must be at least 2 characters long")
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term cannot exceed 100 characters");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Invalid user status");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Invalid user type");

        RuleFor(x => x.Role)
            .MinimumLength(2)
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role name must be at least 2 characters")
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role name cannot exceed 50 characters");

        RuleFor(x => x.City)
            .MinimumLength(2)
            .When(x => !string.IsNullOrWhiteSpace(x.City))
            .WithMessage("City name must be at least 2 characters")
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City))
            .WithMessage("City name cannot exceed 100 characters");

        RuleFor(x => x.Country)
            .MinimumLength(2)
            .When(x => !string.IsNullOrWhiteSpace(x.Country))
            .WithMessage("Country name must be at least 2 characters")
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Country))
            .WithMessage("Country name cannot exceed 100 characters");

        RuleFor(x => x.RegisteredAfter)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.RegisteredAfter.HasValue)
            .WithMessage("RegisteredAfter cannot be in the future");

        RuleFor(x => x.RegisteredBefore)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.RegisteredBefore.HasValue)
            .WithMessage("RegisteredBefore cannot be in the future");

        RuleFor(x => x)
            .Must(x => !x.RegisteredAfter.HasValue || !x.RegisteredBefore.HasValue ||
                      x.RegisteredAfter <= x.RegisteredBefore)
            .WithMessage("RegisteredAfter must be before or equal to RegisteredBefore");

        // Validate sort fields match allowed fields
        RuleFor(x => x.Pagination.SortBy)
            .Must(sortDescriptors => sortDescriptors.All(s => ValidSortFields.Contains(s.FieldName)))
            .WithMessage($"Sort fields must be one of: {string.Join(", ", ValidSortFields)}");
    }
}
