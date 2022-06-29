using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions;

public static class KeyValueStoreExt
{
    public static ListFormat ListFormat { get; } = ListFormat.SlashSeparated;
    public static char Delimiter => ListFormat.Delimiter;

    // Set

    public static Task Set<T>(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, T value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(tenantId, key, value, null, cancellationToken);

    public static Task Set<T>(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var sValue = NewtonsoftJsonSerialized.New(value).Data;
        return keyValueStore.Set(tenantId, key, sValue, expiresAt, cancellationToken);
    }

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, string value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(tenantId, key, value, null, cancellationToken);

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var command = new SetCommand(tenantId, new[] { (key, value, expiresAt) });
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId, 
        (string Key, string Value, Moment? ExpiresAt)[] items,
        CancellationToken cancellationToken = default)
    {
        var command = new SetCommand(tenantId, items);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // Remove

    public static Task Remove(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var command = new RemoveCommand(key);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Remove(this IKeyValueStore keyValueStore,
        Symbol tenantId, string[] keys, CancellationToken cancellationToken = default)
    {
        var command = new RemoveCommand(tenantId, keys);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // TryGet & Get

    public static async ValueTask<Option<T>> TryGet<T>(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(tenantId, key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? Option<T>.None : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    public static async ValueTask<T?> Get<T>(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(tenantId, key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? default : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    // ListKeysByPrefix

    public static Task<string[]> ListKeySuffixes(this IKeyValueStore keyValueStore,
        Symbol tenantId, 
        string prefix,
        PageRef<string> pageRef,
        CancellationToken cancellationToken = default)
        => keyValueStore.ListKeySuffixes(tenantId, prefix, pageRef, SortDirection.Ascending, cancellationToken);
}
