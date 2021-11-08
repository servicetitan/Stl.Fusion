using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Stl.Fusion.EntityFramework;

/// <summary>
/// This type solves a single problem: currently EF Core 6.0
/// doesn't properly dispose pooled DbContexts rendering
/// them unusable after disposal.
/// Details: https://github.com/dotnet/efcore/issues/26202
/// </summary>
public abstract class DbContextBase : DbContext
{
#if NET6_0
    private static readonly Func<DbContext, DbContextLease> LeaseGetter;
    private static readonly Func<DbContext, bool, bool> DisposedSetter;

    static DbContextBase()
    {
        var tDbContext = typeof(DbContext);
        var pDbContext = Expression.Parameter(tDbContext, "dbContext");
        var pValue = Expression.Parameter(typeof(bool), "value");

        var fLease = tDbContext.GetField("_lease", BindingFlags.Instance | BindingFlags.NonPublic);
        LeaseGetter = Expression.Lambda<Func<DbContext, DbContextLease>>(
            Expression.Field(pDbContext, fLease!),
            pDbContext
        ).Compile();

        var fDisposed = tDbContext.GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
        DisposedSetter = Expression.Lambda<Func<DbContext, bool, bool>>(
            Expression.Assign(
                Expression.Field(pDbContext, fDisposed!),
                pValue),
            pDbContext, pValue
        ).Compile();
    }
#endif

    protected DbContextBase() { }
    protected DbContextBase(DbContextOptions options) : base(options) { }

#if NET6_0
    public override void Dispose()
    {
#pragma warning disable EF1001
        var hadActiveLease = LeaseGetter.Invoke(this).IsActive;
        base.Dispose();
        if (!hadActiveLease)
            return;
        var hasActiveLease = LeaseGetter.Invoke(this).IsActive;
        if (hasActiveLease)
            return;
        DisposedSetter.Invoke(this, false);
#pragma warning restore EF1001
    }

    public override async ValueTask DisposeAsync()
    {
#pragma warning disable EF1001
        var hadActiveLease = LeaseGetter.Invoke(this).IsActive;
        await base.DisposeAsync().ConfigureAwait(false);
        if (!hadActiveLease)
            return;
        var hasActiveLease = LeaseGetter.Invoke(this).IsActive;
        if (hasActiveLease)
            return;
        DisposedSetter.Invoke(this, false);
#pragma warning restore EF1001
    }
#endif
}
