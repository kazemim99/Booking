// 📁 Booksy.Core.Domain/Base/ReplaceExpressionVisitor.cs
using System.Linq.Expressions;

namespace Booksy.Core.Domain.Abstractions.Entities;

/// <summary>
/// Helper class for replacing expression parameters - .NET 9 compatible
/// </summary>
internal class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue ?? throw new ArgumentNullException(nameof(oldValue));
        _newValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
    }

    public override Expression? Visit(Expression? node)
    {
        if (node == null) return null;
        return ReferenceEquals(node, _oldValue) ? _newValue : base.Visit(node);
    }
}