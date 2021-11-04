using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions;

public static class KeyValueStoreExt
{
    public static ListFormat ListFormat { get; } = ListFormat.SlashSeparated;
    public static char Delimiter => ListFormat.Delimiter;

    // Set

    public static Task Set<T>(this IKeyValueStore keyValueStore,
        string key, T value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(key, value, null, cancellationToken);

    public static Task Set<T>(this IKeyValueStore keyValueStore,
        string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var sValue = NewtonsoftJsonSerialized.New(value).Data;
        return keyValueStore.Set(key, sValue, expiresAt, cancellationToken);
    }

    public static Task Set(this IKeyValueStore keyValueStore,
        string key, string value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(key, value, null, cancellationToken);

    public static Task Set(this IKeyValueStore keyValueStore,
        string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var command = new SetCommand(key, value, expiresAt).MarkValid();
        return keyValueStore.Set(command, cancellationToken);
    }

    // SetMany

    public static Task SetMany(this IKeyValueStore keyValueStore,
        (string Key, string Value, Moment? ExpiresAt)[] items,
        CancellationToken cancellationToken = default)
    {
        var command = new SetManyCommand(items).MarkValid();
        return keyValueStore.SetMany(command, cancellationToken);
    }

    // Remove

    public static Task Remove(this IKeyValueStore keyValueStore,
        string key, CancellationToken cancellationToken = default)
    {
        var command = new RemoveCommand(key).MarkValid();
        return keyValueStore.Remove(command, cancellationToken);
    }

    // RemoveMany

    public static Task RemoveMany(this IKeyValueStore keyValueStore,
        string[] keys, CancellationToken cancellationToken = default)
    {
        var command = new RemoveManyCommand(keys).MarkValid();
        return keyValueStore.RemoveMany(command, cancellationToken);
    }

    // TryGet & Get

    public static async ValueTask<Option<T>> TryGet<T>(this IKeyValueStore keyValueStore,
        string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? Option<T>.None : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    public static async ValueTask<T?> Get<T>(this IKeyValueStore keyValueStore,
        string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? default : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    // ListKeysByPrefix

    public static Task<string[]> ListKeySuffixes(this IKeyValueStore keyValueStore,
        string prefix,
        PageRef<string> pageRef,
        CancellationToken cancellationToken = default)
        => keyValueStore.ListKeySuffixes(prefix, pageRef, SortDirection.Ascending, cancellationToken);
}
