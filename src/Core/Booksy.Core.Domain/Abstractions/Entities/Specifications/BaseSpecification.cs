using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Entities.Specifications;
using System.Linq.Expressions;

public abstract class BaseSpecification<T> : IAdvancedSpecification<T>
{
    private int _take;
    private int _skip;

    protected BaseSpecification() { }
    protected BaseSpecification(Expression<Func<T, bool>> criteria) => Criteria = criteria;


    private readonly List<OrderExpression<T>> _orderExpressions = new();


    public virtual Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    public virtual void SetCriteria(Expression<Func<T, bool>>? criteria)
    {
        this.Criteria = criteria;
    }
    public int Take => _take;
    public int Skip => _skip;
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; } = true;
    public bool IgnoreQueryFilters { get; private set; }
    public bool IsDistinct { get; private set; }



    /// <summary>
    /// Gets the ordering expressions
    /// </summary>
    public IReadOnlyList<OrderExpression<T>> OrderBy => _orderExpressions.AsReadOnly();

    /// <summary>
    /// Add an ascending order expression
    /// </summary>
    public IOrderableSpecification<T> AddOrderBy(Expression<Func<T, object>> orderExpression)
    {
        _orderExpressions.Clear(); // Clear existing orders for primary order
        _orderExpressions.Add(new OrderExpression<T>(orderExpression, OrderDirection.Ascending, false));
        return this;
    }

    /// <summary>
    /// Add a descending order expression
    /// </summary>
    public IOrderableSpecification<T> AddOrderByDescending(Expression<Func<T, object>> orderExpression)
    {
        _orderExpressions.Clear(); // Clear existing orders for primary order
        _orderExpressions.Add(new OrderExpression<T>(orderExpression, OrderDirection.Descending, false));
        return this;
    }

    /// <summary>
    /// Add a then-by ascending order expression
    /// </summary>
    public IOrderableSpecification<T> AddThenBy(Expression<Func<T, object>> orderExpression)
    {
        _orderExpressions.Add(new OrderExpression<T>(orderExpression, OrderDirection.Ascending, true));
        return this;
    }

    /// <summary>
    /// Add a then-by descending order expression
    /// </summary>
    public IOrderableSpecification<T> AddThenByDescending(Expression<Func<T, object>> orderExpression)
    {
        _orderExpressions.Add(new OrderExpression<T>(orderExpression, OrderDirection.Descending, true));
        return this;
    }


    protected virtual void ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip, nameof(skip));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take, nameof(take));

        _skip = skip;
        _take = take;
        IsPagingEnabled = true;
    }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);
        Includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includeString);
        IncludeStrings.Add(includeString);
    }

    protected virtual void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (Criteria == null)
            Criteria = criteria;
        else
            Criteria = Criteria.And(criteria);
    }

    protected virtual void ApplyCriteria(Expression<Func<T, bool>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        Criteria = criteria;
    }
 
 
    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        ArgumentNullException.ThrowIfNull(groupByExpression);
        GroupBy = groupByExpression;
    }

  
}