using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions.Commands;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.Extensions
{
    public static class IsolatedKeyValueStoreEx
    {
        // Set

        public static Task Set<T>(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, T value, CancellationToken cancellationToken = default)
            => keyValueStore.Set(session, key, value, null, cancellationToken);

        public static Task Set<T>(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var sValue = JsonSerialized.New(value).SerializedValue;
            return keyValueStore.Set(session, key, sValue, expiresAt, cancellationToken);
        }

        public static Task Set(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, string value, CancellationToken cancellationToken = default)
            => keyValueStore.Set(session, key, value, null, cancellationToken);

        public static Task Set(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var command = new IsolatedSetCommand(session, key, value, expiresAt);
            return keyValueStore.Set(command, cancellationToken);
        }

        // SetMany

        public static Task SetMany(this IIsolatedKeyValueStore keyValueStore,
            Session session, (string Key, string Value, Moment? ExpiresAt)[] items,
            CancellationToken cancellationToken = default)
        {
            var command = new IsolatedSetManyCommand(session, items);
            return keyValueStore.SetMany(command, cancellationToken);
        }

        // Remove

        public static Task Remove(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, CancellationToken cancellationToken = default)
        {
            var command = new IsolatedRemoveCommand(session, key);
            return keyValueStore.Remove(command, cancellationToken);
        }

        // RemoveMany

        public static Task RemoveMany(this IIsolatedKeyValueStore keyValueStore,
            Session session, string[] keys, CancellationToken cancellationToken = default)
        {
            var command = new IsolatedRemoveManyCommand(session, keys);
            return keyValueStore.RemoveMany(command, cancellationToken);
        }

        // TryGet

        public static async Task<Option<T>> TryGet<T>(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, CancellationToken cancellationToken = default)
        {
            var sValue = await keyValueStore.TryGet(session, key, cancellationToken).ConfigureAwait(false);
            return sValue == null ? default(Option<T>) : JsonSerialized.New<T>(sValue).Value;
        }

        // Get

        public static async Task<string> Get(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGet(session, key, cancellationToken).ConfigureAwait(false);
            return value ?? throw new KeyNotFoundException();
        }

        public static async Task<T> Get<T>(this IIsolatedKeyValueStore keyValueStore,
            Session session, string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGet<T>(session, key, cancellationToken).ConfigureAwait(false);
            return value.IsSome(out var v) ? v : throw new KeyNotFoundException();
        }

        // ListKeysByPrefix

        public static Task<string[]> ListKeySuffixes(this IIsolatedKeyValueStore keyValueStore,
            Session session,
            string prefix,
            PageRef<string> pageRef,
            CancellationToken cancellationToken = default)
            => keyValueStore.ListKeySuffixes(session, prefix, pageRef, SortDirection.Ascending, cancellationToken);
    }
}
