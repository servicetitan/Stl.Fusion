using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Stl.Fusion.EntityFramework.Internal;

public static class ExpressionExt
{
    public static Expression Replace(this Expression source,
        Expression from, Expression to)
        => new ReplacingExpressionVisitor(new[] { from }, new [] { to }).Visit(source);

    public static Expression Replace(this Expression source,
        Expression from1, Expression to1,
        Expression from2, Expression to2)
        => new ReplacingExpressionVisitor(new[] { from1, from2 }, new [] { to1, to2 }).Visit(source);

    public static Expression Replace(this Expression source,
        Expression from1, Expression to1,
        Expression from2, Expression to2,
        Expression from3, Expression to3)
        => new ReplacingExpressionVisitor(new[] { from1, from2, from3 }, new [] { to1, to2, to3 }).Visit(source);
}
