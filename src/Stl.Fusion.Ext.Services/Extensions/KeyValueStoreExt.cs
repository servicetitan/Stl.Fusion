using System.Diagnostics.CodeAnalysis;

namespace Stl.Fusion.Extensions;

public static class KeyValueStoreExt
{
    public static ListFormat ListFormat { get; set; } = ListFormat.SlashSeparated;
    public static char Delimiter => ListFormat.Delimiter;

    // Set

    public static Task Set<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]T>(
        this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, T value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(tenantId, key, value, null, cancellationToken);

    public static Task Set<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
#pragma warning disable IL2026
        var sValue = NewtonsoftJsonSerialized.New(value).Data;
#pragma warning restore IL2026
        return keyValueStore.Set(tenantId, key, sValue, expiresAt, cancellationToken);
    }

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, string value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(tenantId, key, value, null, cancellationToken);

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var command = new KeyValueStore_Set(tenantId, new[] { (key, value, expiresAt) });
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Set(this IKeyValueStore keyValueStore,
        Symbol tenantId,
        (string Key, string Value, Moment? ExpiresAt)[] items,
        CancellationToken cancellationToken = default)
    {
        var command = new KeyValueStore_Set(tenantId, items);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // Remove

    public static Task Remove(this IKeyValueStore keyValueStore,
        Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var command = new KeyValueStore_Remove(tenantId, new[] { key });
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Remove(this IKeyValueStore keyValueStore,
        Symbol tenantId, string[] keys, CancellationToken cancellationToken = default)
    {
        var command = new KeyValueStore_Remove(tenantId, keys);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // TryGet & Get

    public static async ValueTask<Option<T>> TryGet<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this IKeyValueStore keyValueStore, Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(tenantId, key, cancellationToken).ConfigureAwait(false);
#pragma warning disable IL2026
        return sValue == null ? Option<T>.None : NewtonsoftJsonSerialized.New<T>(sValue).Value;
#pragma warning restore IL2026
    }

    public static async ValueTask<T?> Get<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (this IKeyValueStore keyValueStore, Symbol tenantId, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(tenantId, key, cancellationToken).ConfigureAwait(false);
#pragma warning disable IL2026
        return sValue == null ? default : NewtonsoftJsonSerialized.New<T>(sValue).Value;
#pragma warning restore IL2026
    }

    // ListKeysByPrefix

    public static Task<string[]> ListKeySuffixes(this IKeyValueStore keyValueStore,
        Symbol tenantId,
        string prefix,
        PageRef<string> pageRef,
        CancellationToken cancellationToken = default)
        => keyValueStore.ListKeySuffixes(tenantId, prefix, pageRef, SortDirection.Ascending, cancellationToken);
}
