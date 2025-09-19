using Booksy.Core.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Core.Application.Validators
{
    public class PaginationRequestValidator : AbstractValidator<PaginationRequest>
    {
        public PaginationRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Sort)
                .Must(BeValidSortString)
                .When(x => !string.IsNullOrEmpty(x.Sort))
                .WithMessage("Sort parameter must be in format 'field:direction' (e.g., 'name:asc')");
        }

        private static bool BeValidSortString(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
                return true;

            return sort.Split(',')
                .All(part =>
                {
                    var segments = part.Trim().Split(':');
                    return segments.Length <= 2 &&
                           !string.IsNullOrWhiteSpace(segments[0]) &&
                           (segments.Length == 1 ||
                            segments[1].Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                            segments[1].Equals("desc", StringComparison.OrdinalIgnoreCase));
                });
        }
    }

}
