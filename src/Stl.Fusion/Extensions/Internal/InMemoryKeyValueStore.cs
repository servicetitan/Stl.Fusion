using Stl.Concurrency;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions.Internal;

public class InMemoryKeyValueStore : WorkerBase, IKeyValueStore
{
    public record Options
    {
        public RandomTimeSpan CleanupPeriod { get; init; } = TimeSpan.FromMinutes(1).ToRandom(0.05);
        public IMomentClock? Clock { get; init; } = null;
    }

    protected Options Settings { get; }
    protected IMomentClock Clock { get; }
    protected ConcurrentDictionary<(Symbol TenantId, Symbol Key), (string Value, Moment? ExpiresAt)> Store { get; }

    public InMemoryKeyValueStore(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Clock = settings.Clock ?? services.SystemClock();
        Store = new();
    }

    // Commands

    public virtual Task Set(SetCommand command, CancellationToken cancellationToken = default)
    {
        var items = command.Items;
        var tenantId = command.TenantId;

        if (Computed.IsInvalidating()) {
            foreach (var item in items)
                PseudoGetAllPrefixes(tenantId, item.Key);
            return Task.CompletedTask;
        }

        foreach (var item in items)
            AddOrUpdate(tenantId, item.Key, item.Value, item.ExpiresAt);
        return Task.CompletedTask;
    }

    public virtual Task Remove(RemoveCommand command, CancellationToken cancellationToken = default)
    {
        var keys = command.Keys;
        var tenantId = command.TenantId;

        if (Computed.IsInvalidating()) {
            foreach (var key in keys)
                PseudoGetAllPrefixes(tenantId, key);
            return Task.CompletedTask;
        }

        foreach (var key in keys)
            Store.Remove((TenantId: tenantId, key), out _);
        return Task.CompletedTask;
    }

    // Queries

    public virtual Task<string?> Get(Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        _ = PseudoGet(tenantId, key);
        if (!Store.TryGetValue((tenantId, key), out var item))
            return Task.FromResult((string?) null);
        var expiresAt = item.ExpiresAt;
        if (expiresAt.HasValue && expiresAt.GetValueOrDefault() < Clock.Now)
            return Task.FromResult((string?) null);
        return Task.FromResult((string?) item.Value);
    }

    public virtual Task<int> Count(Symbol tenantId, string prefix, CancellationToken cancellationToken = default)
    {
        // O(Store.Count) cost - definitely not for prod,
        // but fine for client-side use cases & testing.
        _ = PseudoGet(tenantId, prefix);
        var count = Store.Keys
            .Count(k => k.TenantId == tenantId && k.Key.Value.StartsWith(prefix, StringComparison.Ordinal));
        return Task.FromResult(count);
    }

    public virtual Task<string[]> ListKeySuffixes(
        Symbol tenantId,
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        // O(Store.Count) cost - definitely not for prod,
        // but fine for client-side use cases & testing.
        _ = PseudoGet(tenantId, prefix);
        var query = Store.Keys
            .Where(k => k.TenantId == tenantId && k.Key.Value.StartsWith(prefix, StringComparison.Ordinal));
        query = query.OrderByAndTakePage(k => k.Key, pageRef, sortDirection);
        var result = query
            .Select(k => k.Key.Value.Substring(prefix.Length))
            .ToArray();
        return Task.FromResult(result);
    }

    // PseudoXxx query-like methods

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGet(Symbol tenantId, string keyPart) => TaskExt.UnitTask;

    protected void PseudoGetAllPrefixes(Symbol tenantId, string key)
    {
        var delimiter = KeyValueStoreExt.Delimiter;
        var delimiterIndex = key.IndexOf(delimiter, 0);
        for (; delimiterIndex >= 0; delimiterIndex = key.IndexOf(delimiter, delimiterIndex + 1)) {
            var keyPart = key.Substring(0, delimiterIndex);
            _ = PseudoGet(tenantId, keyPart);
        }
        _ = PseudoGet(tenantId, key);
    }

    // Private / protected

    protected bool AddOrUpdate(Symbol tenantId, string key, string value, Moment? expiresAt)
    {
        var spinWait = new SpinWait();
        while (true) {
            if (Store.TryGetValue((tenantId, key), out var item)) {
                if (Store.TryUpdate((tenantId, key), (value, expiresAt), item))
                    return false;
            }
            if (Store.TryAdd((tenantId, key), (value, expiresAt)))
                return true;
            spinWait.SpinOnce();
        }
    }

    // Cleanup

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) {
            await Clock.Delay(Settings.CleanupPeriod.Next(), cancellationToken).ConfigureAwait(false);
            await Cleanup(cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual Task Cleanup(CancellationToken cancellationToken)
    {
        // O(Store.Count) cleanup cost - definitely not for prod,
        // but fine for client-side use cases & testing.
        var now = Clock.Now;
        foreach (var (key, item) in Store) {
            if (!item.ExpiresAt.HasValue)
                continue;
            if (item.ExpiresAt.GetValueOrDefault() < now)
                Store.TryRemove(key, item);
        }
        return Task.CompletedTask;
    }
}
