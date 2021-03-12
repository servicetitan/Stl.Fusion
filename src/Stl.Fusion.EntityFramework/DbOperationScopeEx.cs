using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework
{
    public static class DbOperationScopeEx
    {
        public static Task<DbContext> CreateDbContext(
            this IDbOperationScope scope, CancellationToken cancellationToken = default)
            => scope.CreateDbContext(true, cancellationToken);

        public static Task<TDbContext> CreateDbContext<TDbContext>(
            this DbOperationScope<TDbContext> scope, CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            => scope.CreateDbContext(true, cancellationToken);
    }
}
