using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;

namespace Stl.Fusion.Extensions;

public static class SandboxedKeyValueStoreExt
{
    // Set

    public static Task Set<T>(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, T value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(session, key, value, null, cancellationToken);

    public static Task Set<T>(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var sValue = NewtonsoftJsonSerialized.New(value).Data;
        return keyValueStore.Set(session, key, sValue, expiresAt, cancellationToken);
    }

    public static Task Set(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, string value, CancellationToken cancellationToken = default)
        => keyValueStore.Set(session, key, value, null, cancellationToken);

    public static Task Set(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
    {
        var command = new SandboxedSetCommand(session, new[] { (key, value, expiresAt) });
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Set(this ISandboxedKeyValueStore keyValueStore,
        Session session, (string Key, string Value, Moment? ExpiresAt)[] items,
        CancellationToken cancellationToken = default)
    {
        var command = new SandboxedSetCommand(session, items);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // Remove

    public static Task Remove(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, CancellationToken cancellationToken = default)
    {
        var command = new SandboxedRemoveCommand(session, key);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    public static Task Remove(this ISandboxedKeyValueStore keyValueStore,
        Session session, string[] keys, CancellationToken cancellationToken = default)
    {
        var command = new SandboxedRemoveCommand(session, keys);
        return keyValueStore.GetCommander().Call(command, cancellationToken);
    }

    // TryGet & Get

    public static async ValueTask<Option<T>> TryGet<T>(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(session, key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? Option<T>.None : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    public static async ValueTask<T?> Get<T>(this ISandboxedKeyValueStore keyValueStore,
        Session session, string key, CancellationToken cancellationToken = default)
    {
        var sValue = await keyValueStore.Get(session, key, cancellationToken).ConfigureAwait(false);
        return sValue == null ? default : NewtonsoftJsonSerialized.New<T>(sValue).Value;
    }

    // ListKeysByPrefix

    public static Task<string[]> ListKeySuffixes(this ISandboxedKeyValueStore keyValueStore,
        Session session,
        string prefix,
        PageRef<string> pageRef,
        CancellationToken cancellationToken = default)
        => keyValueStore.ListKeySuffixes(session, prefix, pageRef, SortDirection.Ascending, cancellationToken);
}
