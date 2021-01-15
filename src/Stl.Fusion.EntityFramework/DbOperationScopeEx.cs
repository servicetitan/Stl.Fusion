using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework
{
    public static class DbOperationScopeEx
    {
        public static Task<DbContext> CreateDbContextAsync(this IDbOperationScope scope, CancellationToken cancellationToken = default)
            => scope.CreateDbContextAsync(true, cancellationToken);

        public static Task<TDbContext> CreateDbContextAsync<TDbContext>(this IDbOperationScope<TDbContext> scope, CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            => scope.CreateDbContextAsync(true, cancellationToken);
    }
}
