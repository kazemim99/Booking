using Booksy.Core.Domain.Base;
using System.Linq.Expressions;

namespace Booksy.Core.Domain.Abstractions.Entities.Specifications;

/// <summary>
/// Combines two specifications with AND logic
/// </summary>
public class AndSpecification<T> : BaseSpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override Expression<Func<T, bool>>? Criteria
    {
        get
        {
            var leftExpression = _left.Criteria;
            var rightExpression = _right.Criteria;

            if (leftExpression == null) return rightExpression;
            if (rightExpression == null) return leftExpression;

            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(leftExpression.Parameters[0], parameter);
            var rightVisitor = new ReplaceExpressionVisitor(rightExpression.Parameters[0], parameter);

            var leftBody = leftVisitor.Visit(leftExpression.Body);
            var rightBody = rightVisitor.Visit(rightExpression.Body);

            if (leftBody == null || rightBody == null)
                throw new InvalidOperationException("Expression visitor returned null body");

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(leftBody, rightBody), parameter);
        }
    }
}