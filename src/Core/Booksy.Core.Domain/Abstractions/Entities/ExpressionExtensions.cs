using System.Linq.Expressions;

namespace Booksy.Core.Domain.Abstractions.Entities;

/// <summary>
/// Expression combining utilities for .NET 9
/// </summary>
internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = Expression.Parameter(typeof(T));

        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        if (leftBody == null || rightBody == null)
            throw new InvalidOperationException("Parameter replacement resulted in null expression");

        var andExpression = Expression.AndAlso(leftBody, rightBody);

        return Expression.Lambda<Func<T, bool>>(andExpression, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = Expression.Parameter(typeof(T));

        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);

        // ✅ FIX: Handle nullable return from Visit
        if (leftBody == null || rightBody == null)
            throw new InvalidOperationException("Parameter replacement resulted in null expression");

        var orExpression = Expression.OrElse(leftBody, rightBody);

        return Expression.Lambda<Func<T, bool>>(orExpression, parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return ReferenceEquals(node, oldParam) ? newParam : base.VisitParameter(node);
        }
    }
}