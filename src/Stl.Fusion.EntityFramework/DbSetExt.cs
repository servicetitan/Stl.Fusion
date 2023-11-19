using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework;

public static class DbSetExt
{
    public static DbContext GetDbContext<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this DbSet<T> dbSet)
        where T: class
        => dbSet.GetInfrastructure().GetRequiredService<ICurrentDbContext>().Context;

    public static string GetTableName<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet)
        where T: class
    {
        var dbContext = dbSet.GetDbContext();
        var model = dbContext.Model;
        var entityTypes = model.GetEntityTypes();
        var entityType = entityTypes.Single(t => t.ClrType == typeof(T));
        var tableNameAnnotation = entityType.GetAnnotation("Relational:TableName");
        var tableName = tableNameAnnotation.Value!.ToString();
        return tableName!;
    }

    public static IQueryable<T> WithHints<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, params DbHint[] hints)
        where T: class
    {
        var hintFormatter = dbSet.GetInfrastructure().GetService<IDbHintFormatter>();
        if (hintFormatter == null)
            return dbSet;
        var tableName = dbSet.GetTableName();
        var mHints = MemoryBuffer<DbHint>.Lease(false);
        try {
            mHints.AddSpan(hints.AsSpan());
            if (mHints.Count == 0)
                return dbSet;
            var sql = hintFormatter.FormatSelectSql(tableName, ref mHints);
            return dbSet.FromSqlRaw(sql);
        }
        finally {
            mHints.Release();
        }
    }

    public static IQueryable<T> WithHints<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, DbHint primaryHint, params DbHint[] hints)
        where T: class
    {
        var hintFormatter = dbSet.GetInfrastructure().GetService<IDbHintFormatter>();
        if (hintFormatter == null)
            return dbSet;
        var tableName = dbSet.GetTableName();
        var mHints = MemoryBuffer<DbHint>.Lease(false);
        try {
            mHints.Add(primaryHint);
            mHints.AddSpan(hints.AsSpan());
            if (mHints.Count == 0)
                return dbSet;
            var sql = hintFormatter.FormatSelectSql(tableName, ref mHints);
            return dbSet.FromSqlRaw(sql);
        }
        finally {
            mHints.Release();
        }
    }

    public static IQueryable<T> ForShare<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, params DbHint[] otherHints)
        where T: class
        => dbSet.WithHints(DbLockingHint.Share, otherHints);

    public static IQueryable<T> ForKeyShare<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, params DbHint[] otherHints)
        where T: class
        => dbSet.WithHints(DbLockingHint.KeyShare, otherHints);

    public static IQueryable<T> ForUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, params DbHint[] otherHints)
        where T: class
        => dbSet.WithHints(DbLockingHint.Update, otherHints);

    public static IQueryable<T> ForNoKeyUpdate<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this DbSet<T> dbSet, params DbHint[] otherHints)
        where T: class
        => dbSet.WithHints(DbLockingHint.NoKeyUpdate, otherHints);
}
