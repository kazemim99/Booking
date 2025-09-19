// ✅ IMPROVED: Better extension methods with validation
using Booksy.Core.Domain.Abstractions.Entities.Specifications;
using System.Linq.Expressions;

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new AndSpecification<T>(left, right);
    }

    public static ISpecification<T> Or<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return new OrSpecification<T>(left, right);
    }

    public static ISpecification<T> Not<T>(this ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return new NotSpecification<T>(specification);
    }

    public static ISpecification<T> Empty<T>() => new EmptySpecification<T>();

    public static ISpecification<T> Where<T>(Expression<Func<T, bool>> criteria)
        => new WhereSpecification<T>(criteria);
}

public class EmptySpecification<T> : BaseSpecification<T>
{
    public EmptySpecification() : base(_ => true) { }
}

public class WhereSpecification<T> : BaseSpecification<T>
{
    public WhereSpecification(Expression<Func<T, bool>> criteria) : base(criteria) { }
}