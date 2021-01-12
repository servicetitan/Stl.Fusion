using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework
{
    public static class DbTransactionScopeEx
    {
        public static Task<DbContext> GetDbContextAsync(this IDbTransactionScope scope, CancellationToken cancellationToken = default)
            => scope.GetDbContextAsync(true, cancellationToken);

        public static Task<TDbContext> GetDbContextAsync<TDbContext>(this IDbTransactionScope<TDbContext> scope, CancellationToken cancellationToken = default)
            where TDbContext : DbContext
            => scope.GetDbContextAsync(true, cancellationToken);
    }
}
