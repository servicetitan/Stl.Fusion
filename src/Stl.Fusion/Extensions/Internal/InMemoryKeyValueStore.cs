using Stl.Concurrency;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions.Internal;

public class InMemoryKeyValueStore : WorkerBase, IKeyValueStore
{
    public class Options
    {
        public IMomentClock? Clock { get; set; } = null;
        public TimeSpan CleanupPeriod { get; set; } = TimeSpan.FromMinutes(1);
    }

    protected ConcurrentDictionary<string, (string Value, Moment? ExpiresAt)> Store { get; } = new(StringComparer.Ordinal);
    protected IMomentClock Clock { get; }
    public TimeSpan CleanupPeriod { get; }

    public InMemoryKeyValueStore(Options? options, IServiceProvider services)
    {
        options ??= new();
        CleanupPeriod = options.CleanupPeriod;
        Clock = options.Clock ?? services.SystemClock();
    }

    // Commands

    public virtual Task Set(SetCommand command, CancellationToken cancellationToken = default)
    {
        var (key, value, expiresAt) = command;
        var context = CommandContext.GetCurrent();
        if (string.IsNullOrEmpty(key))
            throw new ArgumentOutOfRangeException($"{nameof(command)}.{nameof(SetCommand.Key)}");
        if (Computed.IsInvalidating()) {
            if (context.Operation().Items.GetOrDefault(true))
                PseudoGetAllPrefixes(key);
            else
                _ = PseudoGet(key);
            return Task.CompletedTask;
        }

        if (!AddOrUpdate(key, value, expiresAt))
            context.Operation().Items.Set(false);
        return Task.CompletedTask;
    }

    public virtual Task SetMany(SetManyCommand command, CancellationToken cancellationToken = default)
    {
        var items = command.Items;
        if (Computed.IsInvalidating()) {
            foreach (var item in items)
                PseudoGetAllPrefixes(item.Key);
            return Task.CompletedTask;
        }

        foreach (var item in items)
            AddOrUpdate(item.Key, item.Value, item.ExpiresAt);
        return Task.CompletedTask;
    }

    public virtual Task Remove(RemoveCommand command, CancellationToken cancellationToken = default)
    {
        var key = command.Key;
        if (string.IsNullOrEmpty(key))
            throw new ArgumentOutOfRangeException($"{nameof(command)}.{nameof(RemoveCommand.Key)}");
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            if (context.Operation().Items.GetOrDefault(true))
                PseudoGetAllPrefixes(key);
            return Task.CompletedTask;
        }

        if (!Store.Remove(key, out _))
            context.Operation().Items.Set(false); // No need to invalidate anything
        return Task.CompletedTask;
    }

    public virtual Task RemoveMany(RemoveManyCommand command, CancellationToken cancellationToken = default)
    {
        var keys = command.Keys;
        if (Computed.IsInvalidating()) {
            foreach (var key in keys)
                PseudoGetAllPrefixes(key);
            return Task.CompletedTask;
        }

        foreach (var key in keys)
            Store.Remove(key, out _);
        return Task.CompletedTask;
    }

    // Queries

    public virtual Task<string?> Get(string key, CancellationToken cancellationToken = default)
    {
        _ = PseudoGet(key);
        if (!Store.TryGetValue(key, out var item))
            return Task.FromResult((string?) null);
        var expiresAt = item.ExpiresAt;
        if (expiresAt.HasValue && expiresAt.GetValueOrDefault() < Clock.Now)
            return Task.FromResult((string?) null);
        return Task.FromResult((string?) item.Value);
    }

    public virtual Task<int> Count(string prefix, CancellationToken cancellationToken = default)
    {
        // O(Store.Count) cost - definitely not for prod,
        // but fine for client-side use cases & testing.
        _ = PseudoGet(prefix);
        var count = Store.Keys.Count(k => k.StartsWith(prefix, StringComparison.Ordinal));
        return Task.FromResult(count);
    }

    public virtual Task<string[]> ListKeySuffixes(
        string prefix,
        PageRef<string> pageRef,
        SortDirection sortDirection = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        // O(Store.Count) cost - definitely not for prod,
        // but fine for client-side use cases & testing.
        _ = PseudoGet(prefix);
        var query = Store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal));
        query = query.OrderByAndTakePage(k => k, pageRef, sortDirection);
        var result = query
            .Select(k => k.Substring(prefix.Length))
            .ToArray();
        return Task.FromResult(result);
    }

    // PseudoXxx query-like methods

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGet(string keyPart) => TaskExt.UnitTask;

    protected void PseudoGetAllPrefixes(string key)
    {
        var delimiter = KeyValueStoreExt.Delimiter;
        var delimiterIndex = key.IndexOf(delimiter, 0);
        for (; delimiterIndex >= 0; delimiterIndex = key.IndexOf(delimiter, delimiterIndex + 1)) {
            var keyPart = key.Substring(0, delimiterIndex);
            _ = PseudoGet(keyPart);
        }
        _ = PseudoGet(key);
    }

    // Private / protected

    protected bool AddOrUpdate(string key, string value, Moment? expiresAt)
    {
        var spinWait = new SpinWait();
        while (true) {
            if (Store.TryGetValue(key, out var item) && Store.TryUpdate(key, (value, expiresAt), item))
                return false;
            if (Store.TryAdd(key, (value, expiresAt)))
                return true;
            spinWait.SpinOnce();
        }
    }

    // Cleanup

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) {
            await Clock.Delay(CleanupPeriod, cancellationToken).ConfigureAwait(false);
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
