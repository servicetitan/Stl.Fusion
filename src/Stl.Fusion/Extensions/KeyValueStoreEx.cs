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
        // SetAsync

        public static Task SetAsync<T>(this IKeyValueStore keyValueStore,
            string key, T value, CancellationToken cancellationToken = default)
            => keyValueStore.SetAsync(key, value, null, cancellationToken);

        public static Task SetAsync<T>(this IKeyValueStore keyValueStore,
            string key, T value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var sValue = JsonSerialized.New(value).SerializedValue;
            return keyValueStore.SetAsync(key, sValue, expiresAt, cancellationToken);
        }

        public static Task SetAsync(this IKeyValueStore keyValueStore,
            string key, string value, CancellationToken cancellationToken = default)
            => keyValueStore.SetAsync(key, value, null, cancellationToken);

        public static Task SetAsync(this IKeyValueStore keyValueStore,
            string key, string value, Moment? expiresAt, CancellationToken cancellationToken = default)
        {
            var command = new SetCommand(key, value, expiresAt).MarkServerSide();
            return keyValueStore.SetAsync(command, cancellationToken);
        }

        // RemoveAsync

        public static Task RemoveAsync(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var command = new RemoveCommand(key).MarkServerSide();
            return keyValueStore.RemoveAsync(command, cancellationToken);
        }

        public static Task RemoveAsync(this IKeyValueStore keyValueStore,
            string[] keys, CancellationToken cancellationToken = default)
        {
            var command = new BulkRemoveCommand(keys).MarkServerSide();
            return keyValueStore.BulkRemoveAsync(command, cancellationToken);
        }

        // TryGetAsync

        public static async Task<Option<T>> TryGetAsync<T>(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var sValue = await keyValueStore.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            return sValue == null ? default(Option<T>) : JsonSerialized.New<T>(sValue).Value;
        }

        // GetAsync

        public static async Task<string> GetAsync(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            return value ?? throw new KeyNotFoundException();
        }

        public static async Task<T> GetAsync<T>(this IKeyValueStore keyValueStore,
            string key, CancellationToken cancellationToken = default)
        {
            var value = await keyValueStore.TryGetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            return value.IsSome(out var v) ? v : throw new KeyNotFoundException();
        }

        // ListKeysByPrefix

        public static Task<string[]> ListKeysByPrefixAsync(this IKeyValueStore keyValueStore,
            string prefix, int limit, CancellationToken cancellationToken = default)
            => keyValueStore.ListKeysByPrefixAsync(prefix, "", limit, cancellationToken);

    }
}
