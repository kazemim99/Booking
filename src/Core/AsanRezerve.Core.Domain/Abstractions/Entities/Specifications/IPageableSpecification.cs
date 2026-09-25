// ✅ BETTER: Separate core from advanced features
using System.Linq.Expressions;

public interface IPageableSpecification<T> : ISpecification<T>
{
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }

    void SetCriteria(Expression<Func<T, bool>>? criteria);

}
