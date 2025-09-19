// ✅ BETTER: Separate core from advanced features
using Booksy.Core.Domain.Abstractions.Entities.Specifications;
using System.Linq.Expressions;

public interface IOrderableSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the ordering expressions for this specification
    /// </summary>
    IReadOnlyList<OrderExpression<T>> OrderBy { get; }

    /// <summary>
    /// Add an ascending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> OrderByAscending(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a descending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> OrderByDescending(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a then-by ascending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> ThenByAscending(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a then-by descending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> ThenByDescending(Expression<Func<T, object>> orderExpression);



}
