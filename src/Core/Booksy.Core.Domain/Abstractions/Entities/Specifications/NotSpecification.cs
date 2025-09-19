using Booksy.Core.Domain.Base;
using System.Linq.Expressions;

namespace Booksy.Core.Domain.Abstractions.Entities.Specifications;

/// <summary>
/// Negates a specification
/// </summary>
public class NotSpecification<T> : BaseSpecification<T>
{
    private readonly ISpecification<T> _specification;

    public NotSpecification(ISpecification<T> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    public override Expression<Func<T, bool>>? Criteria
    {
        get
        {
            var expression = _specification.Criteria;
            if (expression == null) return null; // ✅ Safe handling

            var parameter = Expression.Parameter(typeof(T));
            var visitor = new ReplaceExpressionVisitor(expression.Parameters[0], parameter);

            var body = visitor.Visit(expression.Body);

            if (body == null)
                throw new InvalidOperationException("Expression visitor returned null body");

            return Expression.Lambda<Func<T, bool>>(
                Expression.Not(body), parameter);
        }
    }
}