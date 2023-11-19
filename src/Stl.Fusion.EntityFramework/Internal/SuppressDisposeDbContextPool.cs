using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Stl.Fusion.EntityFramework.Internal;

#pragma warning disable EF1001

internal sealed class SuppressDisposeDbContextPool(IDbContextPoolable dbContextPoolable) : IDbContextPool
{
#if !NETSTANDARD2_0
    public IDbContextPoolable Rent()
        => dbContextPoolable;

    public void Return(IDbContextPoolable context)
    { }

    public ValueTask ReturnAsync(IDbContextPoolable context, CancellationToken cancellationToken = default)
        => default;
#else
    public DbContext Rent()
        => (DbContext)dbContextPoolable;

    public bool Return(DbContext context)
        => true;
#endif
}
