using System;
using System.Collections.Generic;
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
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Extensions
{
    public class DbKeyValueStore<TDbContext, TDbKeyValue> : DbServiceBase<TDbContext>, IKeyValueStore
        where TDbContext : DbContext
        where TDbKeyValue : DbKeyValue, new()
    {
        public IDbEntityResolver<string, TDbKeyValue> KeyValueResolver { get; init; }

        public DbKeyValueStore(IServiceProvider services) : base(services)
            => KeyValueResolver = services.DbEntityResolver<string, TDbKeyValue>();

        // Commands

        public virtual async Task Set(SetCommand command, CancellationToken cancellationToken = default)
        {
            var (key, value, expiresAt) = command;
            var context = CommandContext.GetCurrent();
            if (string.IsNullOrEmpty(key))
                throw new ArgumentOutOfRangeException($"{nameof(command)}.{nameof(SetCommand.Key)}");
            if (Computed.IsInvalidating()) {
                if (context.Operation().Items.GetOrDefault(true))
                    PseudoGetAllPrefixes(key);
                else
                    PseudoGet(key).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var dbKeyValue = await dbContext.FindAsync<TDbKeyValue>(ComposeKey(key), cancellationToken).ConfigureAwait(false);
            if (dbKeyValue == null) {
                dbKeyValue = CreateDbKeyValue(key, value, expiresAt);
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

        public virtual async Task SetMany(SetManyCommand command, CancellationToken cancellationToken = default)
        {
            var items = command.Items;
            if (Computed.IsInvalidating()) {
                foreach (var item in items)
                    PseudoGetAllPrefixes(item.Key);
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var keys = items.Select(i => i.Key).ToList();
            var dbKeyValues = await dbContext.Set<TDbKeyValue>().AsQueryable()
                .Where(e => keys.Contains(e.Key))
                .ToDictionaryAsync(e => e.Key, cancellationToken)
                .ConfigureAwait(false);
            foreach (var item in items) {
                var dbKeyValue = dbKeyValues.GetValueOrDefault(item.Key);
                if (dbKeyValue == null) {
                    dbKeyValue = CreateDbKeyValue(item.Key, item.Value, item.ExpiresAt);
                    dbContext.Add(dbKeyValue);
                }
                else {
                    dbKeyValue.Value = item.Value;
                    dbKeyValue.ExpiresAt = item.ExpiresAt;
                    dbContext.Update(dbKeyValue);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task Remove(RemoveCommand command, CancellationToken cancellationToken = default)
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

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            dbContext.DisableChangeTracking(); // Just to speed up things a bit
            var dbKeyValue = await dbContext.FindAsync<TDbKeyValue>(ComposeKey(key), cancellationToken).ConfigureAwait(false);
            if (dbKeyValue == null) {
                context.Operation().Items.Set(false); // No need to invalidate anything
                return;
            }
            dbContext.Remove(dbKeyValue);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task RemoveMany(RemoveManyCommand command, CancellationToken cancellationToken = default)
        {
            var keys = command.Keys;
            if (Computed.IsInvalidating()) {
                foreach (var key in keys)
                    PseudoGetAllPrefixes(key);
                return;
            }

            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
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

        public virtual async Task<string?> TryGet(string key, CancellationToken cancellationToken = default)
        {
            PseudoGet(key).Ignore();
            var dbKeyValue = await KeyValueResolver.TryGet(key, cancellationToken).ConfigureAwait(false);
            if (dbKeyValue == null)
                return null;
            var expiresAt = dbKeyValue.ExpiresAt;
            if (expiresAt.HasValue && expiresAt.GetValueOrDefault() < Clocks.SystemClock.Now.ToDateTime())
                return null;
            return dbKeyValue?.Value;
        }

        public virtual async Task<int> Count(
            string prefix, CancellationToken cancellationToken = default)
        {
            PseudoGet(prefix).Ignore();
            await using var dbContext = CreateDbContext();
            var count = await dbContext.Set<TDbKeyValue>().AsQueryable()
                .CountAsync(e => e.Key.StartsWith(prefix), cancellationToken)
                .ConfigureAwait(false);
            return count;
        }

        public virtual async Task<string[]> ListKeySuffixes(
            string prefix,
            PageRef<string> pageRef,
            SortDirection sortDirection = SortDirection.Ascending,
            CancellationToken cancellationToken = default)
        {
            PseudoGet(prefix).Ignore();
            await using var dbContext = CreateDbContext();
            var query = dbContext.Set<TDbKeyValue>().AsQueryable()
                .Where(e => e.Key.StartsWith(prefix));
            query = query.OrderByAndTakePage(e => e.Key, pageRef, sortDirection);
            /*
            if (pager.After.IsSome(out var after)) {
                query = sortDirection == SortDirection.Ascending
                    // ReSharper disable once StringCompareIsCultureSpecific.1
                    ? query.Where(e => string.Compare(e.Key, after) > 0)
                    // ReSharper disable once StringCompareIsCultureSpecific.1
                    : query.Where(e => string.Compare(e.Key, after) < 0);
            */
            var result = await query
                .Select(e => e.Key)
                .Take(pageRef.Count)
                .Select(k => k.Substring(prefix.Length))
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        // Protected methods

        [ComputeMethod]
        protected virtual Task<Unit> PseudoGet(string keyPart) => TaskEx.UnitTask;

        protected void PseudoGetAllPrefixes(string key)
        {
            var delimiter = KeyValueStoreEx.Delimiter;
            var delimiterIndex = key.IndexOf(delimiter, 0);
            for (; delimiterIndex >= 0; delimiterIndex = key.IndexOf(delimiter, delimiterIndex + 1)) {
                var keyPart = key.Substring(0, delimiterIndex);
                PseudoGet(keyPart).Ignore();
            }
            PseudoGet(key).Ignore();
        }

        protected virtual TDbKeyValue CreateDbKeyValue(string key, string value, Moment? expiresAt)
            => new() {
                Key = key,
                Value = value,
                ExpiresAt = expiresAt
            };
    }
}
