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
    IOrderableSpecification<T> AddOrderBy(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a descending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> AddOrderByDescending(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a then-by ascending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> AddThenBy(Expression<Func<T, object>> orderExpression);

    /// <summary>
    /// Add a then-by descending order expression
    /// </summary>
    /// <param name="orderExpression">Expression to order by</param>
    /// <returns>The specification for method chaining</returns>
    IOrderableSpecification<T> AddThenByDescending(Expression<Func<T, object>> orderExpression);



}
