using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Stl.Fusion.Extensions
{
    public static class QueryableEx
    {
        public static IQueryable<T> OrderBy<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            SortDirection keySortDirection)
            => keySortDirection == SortDirection.Ascending
                ? source.OrderBy(keySelector)
                : source.OrderByDescending(keySelector);

        public static IQueryable<T> TakePage<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            PageRef<TKey> pageRef,
            SortDirection keySortDirection = SortDirection.Ascending)
        {
            if (pageRef.After.IsSome(out var after)) {
                var pItem = keySelector.Parameters[0];
                var cAfter = Expression.Constant(after);
                var cIntZero = Expression.Constant(0);

                var tComparer = typeof(Comparer<TKey>);
                var pComparerDefault = tComparer.GetProperty(
                    nameof(Comparer<TKey>.Default),
                    BindingFlags.Public | BindingFlags.Static);
                var mCompare = tComparer.GetMethod(
                    nameof(Comparer<TKey>.Default.Compare),
                    new[] {typeof(TKey), typeof(TKey)});

                var eCompare = Expression.Call(
                    Expression.Property(null, pComparerDefault!),
                    mCompare!,
                    keySelector.Body, cAfter);
                var eBody =
                    keySortDirection == SortDirection.Ascending
                        ? Expression.GreaterThan(eCompare, cIntZero)
                        : Expression.LessThan(eCompare, cIntZero);
                source = source.Where(Expression.Lambda<Func<T, bool>>(eBody, pItem));
            }
            return source.Take(pageRef.Count);
        }

        public static IQueryable<T> OrderByAndTakePage<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            PageRef<TKey> pageRef,
            SortDirection keySortDirection = SortDirection.Ascending)
            => source
                .OrderBy(keySelector, keySortDirection)
                .TakePage(keySelector, pageRef, keySortDirection);
    }
}
