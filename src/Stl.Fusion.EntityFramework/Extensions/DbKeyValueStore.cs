using System;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.CommandR;
using Stl.Fusion.Extensions;
using Stl.Fusion.Extensions.Commands;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Extensions
{
    public class DbKeyValueStore<TDbContext, TDbKeyValue> : DbServiceBase<TDbContext>, IKeyValueStore<TDbContext>
        where TDbContext : DbContext
        where TDbKeyValue : DbKeyValue, new()
    {
        public DbEntityResolver<TDbContext, string, TDbKeyValue> DbKeyValueResolver { get; }

        public DbKeyValueStore(IServiceProvider services) : base(services)
            => DbKeyValueResolver = services.GetRequiredService<DbEntityResolver<TDbContext, string, TDbKeyValue>>();

        // Commands

        public virtual async Task SetAsync(SetCommand command, CancellationToken cancellationToken = default)
        {
            var (key, value, expiresAt) = command;
            var context = CommandContext.GetCurrent();
            if (string.IsNullOrEmpty(key))
                throw new ArgumentOutOfRangeException($"{nameof(command)}.{nameof(SetCommand.Key)}");
            if (Computed.IsInvalidating()) {
                if (context.Operation().Items.GetOrDefault(true))
                    PseudoGetAllPrefixes(key);
                else
                    PseudoGetAsync(key).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var dbKeyValue = await dbContext.FindAsync<TDbKeyValue>(ComposeKey(key), cancellationToken).ConfigureAwait(false);
            if (dbKeyValue == null) {
                dbKeyValue = CreateDbKeyValue(command);
                dbContext.Add(dbKeyValue);
            }
            else {
                context.Operation().Items.Set(false); // Don't invalidate prefixes
                dbKeyValue.Value = value;
                dbKeyValue.ExpiresAt = expiresAt;
                dbContext.Update(dbKeyValue);
            }
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }


        public virtual async Task RemoveAsync(RemoveCommand command, CancellationToken cancellationToken = default)
        {
            var key = command.Key;
            if (string.IsNullOrEmpty(key))
                throw new ArgumentOutOfRangeException($"{nameof(command)}.{nameof(RemoveCommand.Key)}");
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                if (context.Operation().Items.GetOrDefault(true))
                    PseudoGetAllPrefixes(key);
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var dbKeyValue = await dbContext.FindAsync<TDbKeyValue>(ComposeKey(key), cancellationToken).ConfigureAwait(false);
            if (dbKeyValue == null) {
                context.Operation().Items.Set(false); // No need to invalidate anything
                return;
            }
            dbContext.Remove(dbKeyValue);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task BulkRemoveAsync(BulkRemoveCommand command, CancellationToken cancellationToken = default)
        {
            var keys = command.Keys;
            if (Computed.IsInvalidating()) {
                foreach (var key in keys)
                    PseudoGetAllPrefixes(key);
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var dbKeyValues = await dbContext.Set<TDbKeyValue>().AsQueryable()
                .Where(e => keys.Contains(e.Key))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var dbKeyValue in dbKeyValues)
                dbContext.Remove(dbKeyValue);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Queries

        public virtual async Task<string?> TryGetAsync(string key, CancellationToken cancellationToken = default)
        {
            PseudoGetAsync(key).Ignore();
            var dbKeyValue = await DbKeyValueResolver.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            return dbKeyValue?.Value;
        }

        public virtual async Task<int> CountByPrefixAsync(
            string prefix, CancellationToken cancellationToken = default)
        {
            PseudoGetAsync(prefix).Ignore();
            await using var dbContext = CreateDbContext();
            var count = await dbContext.Set<TDbKeyValue>().AsQueryable()
                .CountAsync(e => e.Key.StartsWith(prefix), cancellationToken)
                .ConfigureAwait(false);
            return count;
        }

        public virtual async Task<string[]> ListKeysByPrefixAsync(
            string prefix, string startKey, int limit, CancellationToken cancellationToken = default)
        {
            PseudoGetAsync(prefix).Ignore();
            await using var dbContext = CreateDbContext();
            var result = await dbContext.Set<TDbKeyValue>().AsQueryable()
                // ReSharper disable once StringCompareIsCultureSpecific.1
                .Where(e => e.Key.StartsWith(prefix) && string.Compare(e.Key, startKey) > 0)
                .Select(e => e.Key)
                .Take(limit)
                .ToArrayAsync(cancellationToken);
            return result;
        }

        // Protected methods

        [ComputeMethod]
        protected virtual Task<Unit> PseudoGetAsync(string keyPart) => TaskEx.UnitTask;

        protected void PseudoGetAllPrefixes(string key)
        {
            var delimiter = IKeyValueStore.Delimiter;
            var delimiterIndex = key.IndexOf(delimiter, 0);
            for (; delimiterIndex >= 0; delimiterIndex = key.IndexOf(delimiter, delimiterIndex + 1)) {
                var keyPart = key.Substring(0, delimiterIndex);
                PseudoGetAsync(keyPart).Ignore();
            }
            PseudoGetAsync(key).Ignore();
        }

        protected virtual TDbKeyValue CreateDbKeyValue(SetCommand command)
            => new() {
                Key = command.Key,
                Value = command.Value!,
                ExpiresAt = command.ExpiresAt,
            };
    }
}
