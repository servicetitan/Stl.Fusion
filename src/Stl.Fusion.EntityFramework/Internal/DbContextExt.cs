using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Stl.Fusion.EntityFramework.Internal;

public static class DbContextExt
{
    private static readonly FieldInfo LeaseField = typeof(DbContext)
        .GetField("_lease", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void StopPooling(this DbContext dbContext)
    {
#if !NETSTANDARD2_0
#pragma warning disable EF1001
        LeaseField.SetValue(dbContext, DbContextLease.InactiveLease);
#pragma warning restore EF1001
#else
#pragma warning disable EF1001
        ((IDbContextPoolable) dbContext).SetPool(null);
#pragma warning restore EF1001
        LeaseField.SetValue(dbContext, 0);
#endif
    }
}
