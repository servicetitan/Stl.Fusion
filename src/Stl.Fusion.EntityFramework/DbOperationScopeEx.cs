using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework
{
    public static class DbOperationScopeEx
    {
        public static Task<DbContext> GetDbContextAsync(this IDbOperationScope scope, CancellationToken cancellationToken = default)
            => scope.GetDbContextAsync(true, cancellationToken);

        public static Task<TDbContext> GetDbContextAsync<TDbContext>(this IDbOperationScope<TDbContext> scope, CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            => scope.GetDbContextAsync(true, cancellationToken);
    }
}
