// ✅ BETTER: Separate core from advanced features
using System.Linq.Expressions;

public interface IAdvancedSpecification<T> : IOrderableSpecification<T>, IPageableSpecification<T>
{
    Expression<Func<T, object>>? GroupBy { get; }
    bool AsNoTracking { get; }
    bool IgnoreQueryFilters { get; }
    bool IsDistinct { get; }

}