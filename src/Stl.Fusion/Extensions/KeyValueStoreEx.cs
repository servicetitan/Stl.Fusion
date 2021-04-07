using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.Fusion.Extensions.Commands;
using Stl.Serialization;
using Stl.Time;

namespace Stl.Fusion.Extensions
{
    public static class KeyValueStoreEx
    {
        // Set

        public static Task Set<T>(this IKeyValueStore keyValueStore,
            string key, T value, CancellationToken cancellationToken = default)
            => keyValueStore.Set(key, value, null, cancellationToken);

        public static Task Set<T>(this IKeyValueStore keyValueStore,
            string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var sValue = JsonSerialized.New(value).SerializedValue;
            return keyValueStore.Set(key, sValue, expiresAt, cancellationToken);
        }

        public static Task Set(this IKeyValueStore keyValueStore,
            string key, string value, CancellationToken cancellationToken = default)
            => keyValueStore.Set(key, value, null, cancellationToken);

        public static Task Set(this IKeyValueStore keyValueStore,
            string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var command = new SetCommand(key, value, expiresAt).MarkServerSide();
            return keyValueStore.Set(command, cancellationToken);
        }

        // SetMany

        public static Task SetMany(this IKeyValueStore keyValueStore,
            (string Key, string Value, Moment? ExpiresAt)[] items,
            CancellationToken cancellationToken = default)
        {
            var command = new SetManyCommand(items).MarkServerSide();
            return keyValueStore.SetMany(command, cancellationToken);
        }

        // Remove

        public static Task Remove(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var command = new RemoveCommand(key).MarkServerSide();
            return keyValueStore.Remove(command, cancellationToken);
        }

        // RemoveMany

        public static Task RemoveMany(this IKeyValueStore keyValueStore,
            string[] keys, CancellationToken cancellationToken = default)
        {
            var command = new RemoveManyCommand(keys).MarkServerSide();
            return keyValueStore.RemoveMany(command, cancellationToken);
        }

        // TryGet

        public static async Task<Option<T>> TryGet<T>(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var sValue = await keyValueStore.TryGet(key, cancellationToken).ConfigureAwait(false);
            return sValue == null ? default(Option<T>) : JsonSerialized.New<T>(sValue).Value;
        }

        // Get

        public static async Task<string> Get(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGet(key, cancellationToken).ConfigureAwait(false);
            return value ?? throw new KeyNotFoundException();
        }

        public static async Task<T> Get<T>(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGet<T>(key, cancellationToken).ConfigureAwait(false);
            return value.IsSome(out var v) ? v : throw new KeyNotFoundException();
        }

        // ListKeysByPrefix

        public static Task<string[]> ListKeySuffixes(this IKeyValueStore keyValueStore,
            string prefix,
            PageRef<string> pageRef,
            CancellationToken cancellationToken = default)
            => keyValueStore.ListKeySuffixes(prefix, pageRef, SortDirection.Ascending, cancellationToken);
    }
}
